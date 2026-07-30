using System;
using System.Collections.Generic;

namespace StarPlex.Application.Features.Audit.Queries.GetAuditLogs;

public class AuditLogResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PagedAuditLogsResponseDto
{
    public List<AuditLogResponseDto> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}