namespace StarPlex.Application.Common.Interfaces;

public interface ITicketService
{
    Task<byte[]> GenerateTicketPdfAsync(Guid ticketId, CancellationToken ct = default);

    Task<byte[]> GenerateTicketsPdfAsync(List<Guid> ticketIds, CancellationToken ct = default);
}