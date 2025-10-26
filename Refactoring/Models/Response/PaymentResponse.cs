public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public PaymentStatusEnum Status { get; set; }
    public string Message { get; set; } = string.Empty;
}