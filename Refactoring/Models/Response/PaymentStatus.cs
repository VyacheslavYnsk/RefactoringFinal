public class PaymentStatus
{
    public Guid PaymentId { get; set; }
    public PaymentStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}