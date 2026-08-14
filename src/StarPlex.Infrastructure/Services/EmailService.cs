using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Infrastructure.Authentication;

namespace StarPlex.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendTicketEmailAsync(
        string toEmail,
        string recipientName,
        string movieTitle,
        DateTime sessionStartTime,
        string hallName,
        List<string> ticketCodes,
        List<byte[]> ticketPdfs,
        CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(recipientName, toEmail));
        message.Subject = $"🍿 Ваші квитки на фільм {movieTitle} у кінотеатрі {_settings.FromName}!";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1A1A2E;'>Вітаємо, {recipientName}!</h2>
                    <p>Дякуємо за покупку. Оплата пройшла успішно, ваші місця підтверджено.</p>
                    <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;' />
                    <h3 style='color: #E5B842;'>Деталі сеансу:</h3>
                    <p>🎬 <b>Фільм:</b> {movieTitle}</p>
                    <p>📅 <b>Час:</b> {sessionStartTime:dd.MM.yyyy HH:mm}</p>
                    <p>🏛️ <b>Зал:</b> {hallName}</p>
                    <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;' />
                    <p>Електронні квитки з QR-кодами прикріплені до цього листа у форматі PDF. Будь ласка, збережіть їх на телефон та пред'явіть контролеру на вході до зали.</p>
                    <br />
                    <p style='font-size: 12px; color: #999999;'>З повагою, команда {_settings.FromName}.</p>
                </div>"
        };

        for (int i = 0; i < ticketPdfs.Count; i++)
        {
            string code = ticketCodes[i];
            byte[] pdfBytes = ticketPdfs[i];

            bodyBuilder.Attachments.Add($"Ticket-{code}.pdf", pdfBytes, new ContentType("application", "pdf"));
        }

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, MailKit.Security.SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrEmpty(_settings.SmtpUser) && !string.IsNullOrEmpty(_settings.SmtpPassword))
            {
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket email to {RecipientEmail}", toEmail);
            throw;
        }
    }
}