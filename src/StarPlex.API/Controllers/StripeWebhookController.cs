using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBookingFromWebhook;
using Stripe;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(IMediator mediator, IOptions<StripeSettings> settings, ILogger<StripeWebhookController> logger)
    {
        _mediator = mediator;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync();

        try
        {
            var stripeSignature = Request.Headers["Stripe-Signature"];

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _settings.WebhookSecret
            );

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                if (paymentIntent != null && paymentIntent.Metadata.TryGetValue("BookingId", out var bookingIdStr))
                {
                    if (Guid.TryParse(bookingIdStr, out var bookingId))
                    {
                        decimal amount = paymentIntent.Amount / 100m;
                        await _mediator.Send(new ConfirmBookingFromWebhookCommand
                        {
                            BookingId = bookingId,
                            PaymentIntentId = paymentIntent.Id ?? string.Empty,
                            Amount = amount,
                            Currency = paymentIntent.Currency ?? string.Empty
                        });
                    }
                }
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Webhook signature verification failed");
            return BadRequest(new { Message = $"Webhook signature verification failed: {ex.Message}" });
        }
    }
}