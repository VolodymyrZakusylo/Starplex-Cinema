using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Infrastructure.Configuration;
using StarPlex.Infrastructure.Authentication;

namespace StarPlex.Infrastructure.Services;

public class AzureEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly AzureEmailSettings _settings;
    private readonly EmailSettings _coreSettings;
    private readonly ILogger<AzureEmailService> _logger;

    public AzureEmailService(
        EmailClient emailClient,
        IOptions<AzureEmailSettings> settings,
        IOptions<EmailSettings> coreSettings,
        ILogger<AzureEmailService> logger)
    {
        _emailClient = emailClient;
        _settings = settings.Value;
        _coreSettings = coreSettings.Value;
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
        var subject = $"🍿 Ваші квитки на фільм {movieTitle} у кінотеатрі {_coreSettings.FromName}!";
        
        var htmlContent = $@"
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
                    <p style='font-size: 12px; color: #999999;'>З повагою, команда {_coreSettings.FromName}.</p>
                </div>";

        var emailContent = new EmailContent(subject)
        {
            Html = htmlContent
        };

        var emailRecipients = new EmailRecipients(new List<EmailAddress> { new EmailAddress(toEmail, recipientName) });
        var emailMessage = new EmailMessage(
            senderAddress: _settings.SenderAddress,
            recipients: emailRecipients,
            content: emailContent);

        for (int i = 0; i < ticketPdfs.Count; i++)
        {
            var pdfBytes = ticketPdfs[i];
            var code = ticketCodes[i];
            
            var attachment = new EmailAttachment(
                $"Ticket-{code}.pdf",
                "application/pdf",
                new BinaryData(pdfBytes));

            emailMessage.Attachments.Add(attachment);
        }

        try
        {
            await _emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket email via ACS to {RecipientEmail}", toEmail);
            throw;
        }
    }
}
