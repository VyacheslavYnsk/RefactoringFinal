using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class HallService : IHallService
{
    private readonly ApplicationDbContext _context;
    private readonly ISeatService _seatService; 
    private static Dictionary<Guid, Hall> _hallCache = new();

    
    public HallService(ApplicationDbContext context, ISeatService seatService = null, IConfiguration configuration = null)
    {
        _context = context;
        _seatService = seatService;
    }

    
    public async Task<Hall> CreateAsync(HallCreate hallCreate)
    {
        
        if (hallCreate == null)
            throw new ArgumentNullException(nameof(hallCreate));
        
        if (string.IsNullOrWhiteSpace(hallCreate.Name))
            throw new ArgumentException("Hall name is required", nameof(hallCreate.Name));
        
        if (hallCreate.Number <= 0)
            throw new ArgumentException("Hall number must be positive", nameof(hallCreate.Number));

        
        var existingHall = await _context.Halls
            .FirstOrDefaultAsync(h => h.Number == hallCreate.Number);

        if (existingHall != null)
        {
            throw new InvalidOperationException($"Зал с номером {hallCreate.Number} уже существует");
        }

        
        var hall = new HallDto
        {
            Id = Guid.NewGuid(),
            Name = hallCreate.Name,
            Number = hallCreate.Number,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Rows = 0,
        };

        try
        {
            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();

            
            _hallCache[hall.Id] = CreateHallResponse(hall);

            
            var hallResp = new Hall
            {
                Id = hall.Id,
                Name = hall.Name,
                Number = hall.Number,
                CreatedAt = hall.CreatedAt,
                UpdatedAt = hall.UpdatedAt,

            };

            return hallResp;
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Error creating hall: {ex.Message}");
            throw;
        }
    }

    
    public async Task<PaginationResponse<Hall>> GetListAsync(int page, int size)
    {
        
        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        
        var query = _context.Halls
            .OrderBy(h => h.Number)
            .Select(u => new Hall
            {
                Id = u.Id,
                Name = u.Name,
                Number = u.Number,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,

            });

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)size);
        var skip = page * size;

        
        var data = await query
            .Skip(skip)
            .Take(size)
            .ToListAsync();

        
        return new PaginationResponse<Hall>
        {
            Data = data,
            Pagination = new PaginationInfo
            {
                Page = page,
                Limit = size,
                Total = total,
                Pages = totalPages,
            }
        };
    }

    
    public async Task<Hall> GetByIdAsync(Guid id)
    {
        
        if (_hallCache.ContainsKey(id))
        {
            return _hallCache[id];
        }

        var hall = await _context.Halls.FindAsync(id);

        if (hall == null)
        {
            throw new KeyNotFoundException($"Зал с ID {id} не найден");
        }

        
        var hallResp = new Hall
        {
            Id = hall.Id,
            Name = hall.Name,
            Number = hall.Number,
            CreatedAt = hall.CreatedAt,
            UpdatedAt = hall.UpdatedAt,

        };

        _hallCache[id] = hallResp;

        return hallResp;
    }

    public async Task<Hall> EditAsync(HallUpdate hallUpdate, Guid id)
    {

        if (hallUpdate == null)
            throw new ArgumentNullException(nameof(hallUpdate));

        if (id == Guid.Empty)
            throw new ArgumentException("Invalid hall ID", nameof(id));

        var hall = await _context.Halls.FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null)
        {
            throw new KeyNotFoundException($"Зал с ID {id} не найден");
        }

        bool hasChanges = false;

        if (hallUpdate.Number.HasValue)
        {
            if (hallUpdate.Number.Value != hall.Number)
            {
                var existingHall = await _context.Halls
                    .FirstOrDefaultAsync(h => h.Number == hallUpdate.Number.Value && h.Id != id);

                if (existingHall != null)
                {
                    throw new InvalidOperationException($"Зал с номером {hallUpdate.Number.Value} уже существует");
                }

                hall.Number = hallUpdate.Number.Value;
                hasChanges = true;
            }
        }

        if (!string.IsNullOrEmpty(hallUpdate.Name) && hallUpdate.Name != hall.Name)
        {
            hall.Name = hallUpdate.Name;
            hasChanges = true;
        }

        hall.UpdatedAt = DateTime.UtcNow;
        
        var hallResp = new Hall
        {
            Id = hall.Id,
            Name = hall.Name,
            Number = hall.Number,
            CreatedAt = hall.CreatedAt,
            UpdatedAt = hall.UpdatedAt,
        };

        await _context.SaveChangesAsync();

        _hallCache.Remove(id);

        return hallResp;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var hall = await _context.Halls
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null)
        {
            throw new KeyNotFoundException($"Зал с ID {id} не найден");
        }

        
        try
        {
            _context.Halls.Remove(hall);
            var result = await _context.SaveChangesAsync();

            _hallCache.Remove(id);

            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting hall {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<int> GetHallsRowsAsync(Guid id)
    {
        var hall = await _context.Halls.FindAsync(id);

        if (hall == null)
        {
            throw new KeyNotFoundException($"Зал с ID {id} не найден");
        }

        return hall.Rows;
    }
    
    public async Task<int> EditRowsCountAsync(int newRows, Guid id)
    {
        var hall = await _context.Halls.FindAsync(id);

        if (hall == null)
        {
            throw new KeyNotFoundException($"Зал с ID {id} не найден");
        }

        if (newRows < 0)
            throw new ArgumentException("Rows count cannot be negative");

        if (newRows > 1000)
            throw new ArgumentException("Too many rows");

        hall.Rows = newRows;
        hall.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _hallCache.Remove(id);

        return hall.Rows;        
    }

    private Hall CreateHallResponse(HallDto hallEntity)
    {
        return new Hall
        {
            Id = hallEntity.Id,
            Name = hallEntity.Name,
            Number = hallEntity.Number,
            CreatedAt = hallEntity.CreatedAt,
            UpdatedAt = hallEntity.UpdatedAt
        };
    }

    private async Task LogHallChange(Guid hallId, string changeType)
    {
        Console.WriteLine($"Hall {hallId} changed: {changeType} at {DateTime.UtcNow}");
    }

    public async Task<List<Hall>> SearchHallsAsync(string searchTerm)
    {
        await Task.Delay(1);
        return new List<Hall>();
    }

    public async Task<bool> ValidateHallConfigurationAsync(Guid hallId)
    {
        return true;
    }
}