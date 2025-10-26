public class SeatDto : Entity
{
    public required int Row { get; set; }

    public required int Number { get; set; }

    public required Guid CategotyId { get; set; }

    public required Status Status { get; set; }

    public Guid HallId { get; set; }

}