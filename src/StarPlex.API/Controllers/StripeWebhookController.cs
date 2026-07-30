using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using Stripe;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly StripeSettings _settings;

    public StripeWebhookController(IMediator mediator, IOptions<StripeSettings> settings)
    {
        _mediator = mediator;
        _settings = settings.Value;
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
                        await _mediator.Send(new ConfirmBookingCommand { BookingId = bookingId });
                    }
                }
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            return BadRequest(new { Message = $"Webhook signature verification failed: {ex.Message}" });
        }
    }
}