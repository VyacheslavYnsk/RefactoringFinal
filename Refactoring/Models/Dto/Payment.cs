public class Payment : Entity
{
    public Guid ClientId { get; set; }
    public Guid PurchaseId { get; set; }

    public PaymentStatusEnum Status { get; set; } = PaymentStatusEnum.PENDING;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
