public interface ITicketService
{
    Task<bool> CreateTicketAsync(List<Seat> seats, List<SeatCategory> seatCategories, Guid sessionId);
    Task<int> DeleteTicketsBySessionAsync(Guid sessionId);

    Task<List<Ticket>> GetTicketsBySessions(Guid sessionId, Status? status);

    Task<Ticket> ReserveTicketAsync(Guid userId, Guid ticketId);

    Task<Ticket> CancelReservationTicketAsync(Guid userId, Guid ticketId);



}