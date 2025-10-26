public interface IPaymentService
{
    Task<PaymentResponse> ProcessAsync(Guid clientId, PaymentRequest request);
    Task<PaymentStatus?> GetStatusAsync(Guid paymentId);
}
