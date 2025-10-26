using System.ComponentModel.DataAnnotations;

public class PurchaseCreate
{
    [Required]
    public List<Guid> TicketIds { get; set; } = new();
}
