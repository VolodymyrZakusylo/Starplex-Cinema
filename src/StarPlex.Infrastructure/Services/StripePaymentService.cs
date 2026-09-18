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

    public async Task<string> CreatePaymentIntentAsync(Guid bookingId, decimal amount, string currency = "uah", string? idempotencyKey = null, CancellationToken ct = default)
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

        var requestOptions = new RequestOptions();
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            requestOptions.IdempotencyKey = idempotencyKey;
        }

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, requestOptions, cancellationToken: ct);

        return intent.ClientSecret;
    }

    public async Task<bool> VerifyPaymentIntentAsync(string paymentIntentId, Guid bookingId, decimal expectedAmount, string currency = "uah", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId)) return false;

        try
        {
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(paymentIntentId, cancellationToken: ct);

            if (intent == null) return false;
            if (intent.Status != "succeeded") return false;
            if (!intent.Metadata.TryGetValue("BookingId", out var metadataBookingId) || !Guid.TryParse(metadataBookingId, out var parsedBookingId) || parsedBookingId != bookingId) return false;

            long expectedAmountCents = (long)(expectedAmount * 100);
            if (intent.Amount != expectedAmountCents) return false;
            if (!string.Equals(intent.Currency, currency, StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Stripe PaymentIntent {PaymentIntentId} for booking {BookingId}", paymentIntentId, bookingId);
            return false;
        }
    }

    public async Task<PaymentIntentExpiryResult> ExpirePaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId)) return PaymentIntentExpiryResult.Indeterminate;

        try
        {
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(paymentIntentId, cancellationToken: ct);

            if (intent?.Status == "succeeded") return PaymentIntentExpiryResult.AlreadySucceeded;
            if (intent?.Status == "canceled") return PaymentIntentExpiryResult.Cancelled;
            if (intent == null) return PaymentIntentExpiryResult.Indeterminate;

            intent = await service.CancelAsync(paymentIntentId, cancellationToken: ct);
            return intent?.Status switch
            {
                "canceled" => PaymentIntentExpiryResult.Cancelled,
                "succeeded" => PaymentIntentExpiryResult.AlreadySucceeded,
                _ => PaymentIntentExpiryResult.Indeterminate
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not safely expire Stripe PaymentIntent {PaymentIntentId}", paymentIntentId);
            return PaymentIntentExpiryResult.Indeterminate;
        }
    }

    public async Task<bool> RefundPaymentAsync(string paymentIntentId, decimal amount, string currency = "uah", CancellationToken ct = default, string? idempotencyKey = null)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(amount * 100),
                Reason = RefundReasons.RequestedByCustomer
            };

            var requestOptions = new RequestOptions();
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                requestOptions.IdempotencyKey = idempotencyKey;
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options, requestOptions, cancellationToken: ct);

            return refund.Status == "succeeded" || refund.Status == "pending";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund payment {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<bool> UpdatePaymentIntentAmountAsync(string paymentIntentId, decimal amount, string currency = "uah", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId)) return false;

        try
        {
            var options = new PaymentIntentUpdateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLower()
            };

            var service = new PaymentIntentService();
            var intent = await service.UpdateAsync(paymentIntentId, options, cancellationToken: ct);

            return intent != null && intent.Amount == (long)(amount * 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Stripe PaymentIntent {PaymentIntentId} amount to {Amount}", paymentIntentId, amount);
            return false;
        }
    }
}
