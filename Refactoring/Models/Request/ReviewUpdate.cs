using System.ComponentModel.DataAnnotations;

public class ReviewUpdate
{
    [Range(1, 5)]
    public int? Rating { get; set; }

    [MaxLength(1000)]
    public string? Text { get; set; }
}