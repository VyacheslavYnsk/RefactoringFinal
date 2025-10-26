using System.ComponentModel.DataAnnotations;

public class ReviewCreate
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; }
}