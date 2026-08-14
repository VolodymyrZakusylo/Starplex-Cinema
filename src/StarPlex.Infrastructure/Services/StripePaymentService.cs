using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using Stripe;

namespace StarPlex.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IOptions<StripeSettings> settings, ILogger<StripePaymentService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<string> CreatePaymentIntentAsync(Guid bookingId, decimal amount, string currency = "uah", CancellationToken ct = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency.ToLower(),
            PaymentMethodTypes = new List<string> { "card" },
            Metadata = new Dictionary<string, string>
            {
                { "BookingId", bookingId.ToString() }
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, cancellationToken: ct);

        return intent.ClientSecret;
    }

    public async Task<bool> RefundPaymentAsync(string paymentIntentId, decimal amount, string currency = "uah", CancellationToken ct = default)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(amount * 100),
                Reason = RefundReasons.RequestedByCustomer
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: ct);

            return refund.Status == "succeeded" || refund.Status == "pending";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund payment {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }
}