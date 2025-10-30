using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SeatService : ISeatService
{
    private readonly ApplicationDbContext _context;

    public SeatService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> CreateSeatListAsync(List<SeatCreate> seatCreates, Guid hallId, int rows)
    {
        if (rows <= 0)
        throw new ArgumentException("Количество рядов должно быть положительным числом", nameof(rows));

        var invalidRowSeats = seatCreates.Where(s => s.Row > rows || s.Row <= 0).ToList();
        if (invalidRowSeats.Any())
        {
            var invalidRows = invalidRowSeats.Select(s => s.Row).Distinct();
            throw new ArgumentException(
                $"Некорректные номера рядов: {string.Join(", ", invalidRows)}. Допустимый диапазон: 1-{rows}");
        }

        if (hallId == Guid.Empty)
            throw new ArgumentException("HallId не может быть пустым");

        if (seatCreates == null || !seatCreates.Any())
            throw new ArgumentException("Некорректный запрос");


        var duplicateSeats = seatCreates
            .GroupBy(s => new { s.Row, s.Number })
            .Where(g => g.Count() > 1)
            .Select(g => $"Ряд: {g.Key.Row}, Место: {g.Key.Number}")
            .ToList();

        if (duplicateSeats.Any())
        {
            throw new InvalidOperationException($"Обнаружены дублирующиеся места: {string.Join("\n", duplicateSeats)}");
        }    

        var seats = new List<Seat>();
        
        foreach (var seatCreate in seatCreates)
        {
            var check = await _context.Seats.AnyAsync(s => s.HallId == hallId && s.Number == seatCreate.Number && s.Row == seatCreate.Row);
            if (!check)
            {



                var seatDto = new SeatDto
                {
                    Id = Guid.NewGuid(),
                    Row = seatCreate.Row,
                    CategotyId = seatCreate.CategoryId,
                    Number = seatCreate.Number,
                    Status = Status.Available,
                    HallId = hallId
                };

                var seat = new Seat
                {
                    Id = seatDto.Id,
                    Row = seatDto.Row,
                    Number = seatDto.Number,
                    CategotyId = seatDto.CategotyId,
                    Status = Status.Available,
                };

                seats.Add(seat);

                _context.Seats.Add(seatDto);
            }
            
        }

        await _context.SaveChangesAsync();
        return seats;
    }

    public async Task<List<Seat>> GetSeatsByHallIdAsync(Guid hallId)
    {
        if (hallId == Guid.Empty)
            throw new ArgumentException("Hall ID не может быть пустым");

        return  await _context.Seats
            .Where(s => s.HallId == hallId)
            .Select(u => new Seat
            {
                Id = u.Id,
                Row = u.Row,
                Number = u.Number,
                CategotyId = u.CategotyId,
                Status = u.Status,
            }).ToListAsync();
    }

}