using System.ComponentModel.DataAnnotations;

public class SessionCreate
{
    [Required]
    public Guid FilmId { get; set; }

    [Required]
    public Guid HallId { get; set; }

    [Required]
    public DateTime StartAt { get; set; }
}
