using System.ComponentModel.DataAnnotations;

public class PaymentRequest
{
    [Required]
    public Guid PurchaseId { get; set; }

    [Required]
    [CreditCard]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    public string ExpiryDate { get; set; } = string.Empty; 

    [Required]
    [StringLength(4, MinimumLength = 3)]
    public string CVV { get; set; } = string.Empty;

    [Required]
    public string CardHolderName { get; set; } = string.Empty;
}