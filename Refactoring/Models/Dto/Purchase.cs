using System.ComponentModel.DataAnnotations;

public class Purchase : Entity
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public List<Guid> TicketIds { get; set; } = new();

    [Required]
    public int TotalCents { get; set; }

    [Required]
    public PurchaseStatus Status { get; set; } = PurchaseStatus.PENDING;

    public DateTime CreatedAt { get; set; }
}
