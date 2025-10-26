public class HallPlan
{
    public required Guid HallId { get; set; }

    public required int Rows { get; set; }

    public required List<Seat> Seats { get; set; }
    
    public required List<SeatCategory> Categories { get; set; }
}