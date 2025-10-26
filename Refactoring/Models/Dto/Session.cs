using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public class Session : Entity
{
    [Required]
    public Guid FilmId { get; set; }

    [Required]
    public Guid HallId { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public Timeslot Timeslot { get; set; } = null!;
}

[Owned]
public class Timeslot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}