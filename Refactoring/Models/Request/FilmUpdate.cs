using System.ComponentModel.DataAnnotations;

public class FilmUpdate
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? DurationMinutes { get; set; }

    public AgeRating? AgeRating { get; set; }
}