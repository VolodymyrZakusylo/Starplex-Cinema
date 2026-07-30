namespace StarPlex.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendTicketEmailAsync(
        string toEmail,
        string recipientName,
        string movieTitle,
        DateTime sessionStartTime,
        string hallName,
        List<string> ticketCodes,
        List<byte[]> ticketPdfs,
        CancellationToken ct);
}