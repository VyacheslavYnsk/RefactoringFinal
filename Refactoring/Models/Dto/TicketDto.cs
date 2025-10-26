public class TicketDto : Entity
{
    public required Guid SessionId { get; set; }
    public required Guid SeatId { get; set; }

    public required Guid CategoryId { get; set; }

    public int PriceCents { get; set; }

    public Status Status { get; set; }

    public DateTime? ReservedUntil { get; set; }

    public Guid? BuyerId { get; set; }

}