using System.ComponentModel.DataAnnotations;

public class Review : Entity
{
    [Required]
    public Guid FilmId { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
