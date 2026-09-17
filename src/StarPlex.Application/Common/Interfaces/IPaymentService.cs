namespace StarPlex.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<string> CreatePaymentIntentAsync(Guid bookingId, decimal amount, string currency = "uah", string? idempotencyKey = null, CancellationToken ct = default);

    Task<bool> RefundPaymentAsync(string paymentIntentId, decimal amount, string currency = "uah", CancellationToken ct = default);
}