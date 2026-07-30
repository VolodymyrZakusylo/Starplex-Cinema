using MediatR;

namespace StarPlex.Application.Features.Audit.Queries.GetAuditLogs;

public class GetAuditLogsQuery : IRequest<PagedAuditLogsResponseDto>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public string? SearchUser { get; set; }
    public string? EntityName { get; set; }
    public string? ActionType { get; set; }
}