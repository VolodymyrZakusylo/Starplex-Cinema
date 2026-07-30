using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Audit.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedAuditLogsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetAuditLogsQueryHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<PagedAuditLogsResponseDto> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityName) && request.EntityName != "ALL")
        {
            query = query.Where(l => l.EntityName == request.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType) && request.ActionType != "ALL")
        {
            if (request.ActionType == "REFUND")
            {
                query = query.Where(l => l.Action.Contains("TicketRefund"));
            }
            else
            {
                query = query.Where(l => l.Action == request.ActionType && !l.Action.Contains("TicketRefund"));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchUser))
        {
            query = query.Where(l => l.UserId.ToString().Contains(request.SearchUser.ToLower()));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = logs.Where(l => l.UserId != Guid.Empty).Select(l => l.UserId).Distinct().ToList();
        var userEmailsMap = new Dictionary<Guid, string>();

        foreach (var userId in userIds)
        {
            var userInfo = await _userService.GetUserContactInfoAsync(userId, cancellationToken);
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.Value.Email))
            {
                userEmailsMap[userId] = userInfo.Value.Email;
            }
        }

        var items = logs.Select(log =>
        {
            string email = "System Event";
            if (log.UserId != Guid.Empty)
            {
                email = userEmailsMap.TryGetValue(log.UserId, out var userEmail)
                    ? userEmail
                    : $"Staff ID: {log.UserId.ToString()[..8].ToUpper()}";
            }

            return new AuditLogResponseDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = email,
                Action = log.Action,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Timestamp = log.Timestamp
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedAuditLogsResponseDto
        {
            Items = items,
            PageNumber = request.PageNumber,
            TotalPages = totalPages == 0 ? 1 : totalPages,
            TotalCount = totalCount
        };
    }
}