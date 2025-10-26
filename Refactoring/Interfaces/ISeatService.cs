public interface ISeatService
{
    Task<List<Seat>> GetSeatsByHallIdAsync(Guid hallId);

    Task<List<Seat>> CreateSeatListAsync(List<SeatCreate> seatCreates, Guid hallId, int rows);

}