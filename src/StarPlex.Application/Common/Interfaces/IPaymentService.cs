using StarPlex.Application.Common.Models;

namespace StarPlex.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<string> CreatePaymentIntentAsync(Guid bookingId, decimal amount, string currency = "uah", string? idempotencyKey = null, CancellationToken ct = default);

    Task<bool> VerifyPaymentIntentAsync(string paymentIntentId, Guid bookingId, decimal expectedAmount, string currency = "uah", CancellationToken ct = default);

    Task<bool> RefundPaymentAsync(string paymentIntentId, decimal amount, string currency = "uah", CancellationToken ct = default, string? idempotencyKey = null);

    Task<bool> UpdatePaymentIntentAmountAsync(string paymentIntentId, decimal amount, string currency = "uah", CancellationToken ct = default);

    Task<PaymentIntentExpiryResult> ExpirePaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
}
