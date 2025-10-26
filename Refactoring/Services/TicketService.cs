using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;

    public TicketService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateTicketAsync(List<Seat> seats, List<SeatCategory> seatCategories, Guid sessionId)
    {
        var categoryDict = seatCategories.ToDictionary(sc => sc.Id, sc => sc);

        var tickets = new List<TicketDto>();

        foreach (var seat in seats)
        {
            if (categoryDict.TryGetValue(seat.CategotyId, out var seatCategory))
            {
                var ticket = new TicketDto
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    CategoryId = seatCategory.Id,
                    SeatId = seat.Id,
                    PriceCents = seatCategory.PriceCents,
                    Status = Status.Available
                };
                tickets.Add(ticket);
            }
        }

        _context.Tickets.AddRange(tickets);

        var result = await _context.SaveChangesAsync();

        return result > 0;
    }

    public async Task<int> DeleteTicketsBySessionAsync(Guid sessionId)
    {
        var ticketsToDelete = _context.Tickets
            .Where(t => t.SessionId == sessionId);

        _context.Tickets.RemoveRange(ticketsToDelete);

        return await _context.SaveChangesAsync();
    }


    public async Task<List<Ticket>> GetTicketsBySessions(Guid sessionId, Status? status)
    {
        try
        {
            var query = _context.Tickets
                .Where(t => t.SessionId == sessionId)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            return await query
                .Select(t => new Ticket
                {
                    Id = t.Id,
                    SessionId = t.SessionId,
                    SeatId = t.SeatId,
                    CategoryId = t.CategoryId,
                    PriceCents = t.PriceCents,
                    Status = t.Status,
                    ReservedUntil = t.ReservedUntil
                })
                .OrderBy(t => t.SeatId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при получении тикетов для сессии {sessionId}", ex);
        }
    }

    public async Task<Ticket?> ReserveTicketAsync(Guid userId, Guid ticketId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
                throw new ArgumentException($"Билет {ticketId} не найден", nameof(ticketId));

            if (ticket.Status != Status.Available)
                throw new InvalidOperationException($"Билет {ticketId} недоступен для бронирования");

            ticket.BuyerId = userId;
            ticket.Status = Status.Reserved;
            ticket.ReservedUntil = DateTime.UtcNow.AddMinutes(20);



            var ticketResponse = new Ticket
            {
                Id = ticket.Id,
                SessionId = ticket.SessionId,
                SeatId = ticket.SeatId,
                CategoryId = ticket.CategoryId,
                PriceCents = ticket.PriceCents,
                Status = ticket.Status,
                ReservedUntil = ticket.ReservedUntil
            };

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ticketResponse;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Ticket?> CancelReservationTicketAsync(Guid userId, Guid ticketId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                    throw new ArgumentException($"Билет {ticketId} не найден", nameof(ticketId));
                    
                if (ticket.Status != Status.Reserved)
                    throw new InvalidOperationException($"Билет {ticketId} недоступен для отмены бронирования");

                if (ticket.BuyerId != userId)
                throw new InvalidOperationException($"Пользователь не имеет право отменить бронирование билета: {ticketId}");

                ticket.BuyerId = null;
                ticket.Status = Status.Available;
                ticket.ReservedUntil = null;

                

                var ticketResponse = new Ticket
                {
                    Id = ticket.Id,
                    SessionId = ticket.SessionId,
                    SeatId = ticket.SeatId,
                    CategoryId = ticket.CategoryId,
                    PriceCents = ticket.PriceCents,
                    Status = ticket.Status,
                    ReservedUntil = ticket.ReservedUntil
                };

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ticketResponse;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
    }







}