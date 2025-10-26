using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SeatCategoryService : ISeatCategoryService
{
    private readonly ApplicationDbContext _context;

    public SeatCategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatCategory> CreateAsync(SeatCategoryCreate seatCategoryCreate)
    {

        var seatCategory = new SeatCategory
        {
            Id = Guid.NewGuid(),
            Name = seatCategoryCreate.Name,
            PriceCents = seatCategoryCreate.PriceCents,
        };

        _context.SeatCategories.Add(seatCategory);

        await _context.SaveChangesAsync();

        return seatCategory;
    }

    public async Task<PaginationResponse<SeatCategory>> GetListAsync(int page, int size)
    {
        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        var query = _context.SeatCategories.OrderBy(h => h.Name);

        var total = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(total / (double)size);

        var skip = page * size;

        var data = await query
            .Skip(skip)
            .Take(size)
            .ToListAsync();

        return new PaginationResponse<SeatCategory>
        {
            Data = data,
            Pagination = new PaginationInfo
            {
                Page = page,
                Limit = size,
                Total = total,
                Pages = totalPages
            }
        };
    }

    public async Task<SeatCategory> GetByIdAsync(Guid id)
    {
        var seatCategory = await _context.SeatCategories.FindAsync(id);

        if (seatCategory == null)
        {
            throw new KeyNotFoundException($"Категория с ID {id} не найден");
        }

        return seatCategory;
    }

    public async Task<SeatCategory> EditAsync(SeatCategoryUpdate seatCategoryUpdate, Guid id)
    {

        var seatCategory = await _context.SeatCategories.FirstOrDefaultAsync(h => h.Id == id);

        if (seatCategory == null)
        {
            throw new KeyNotFoundException($"Категория с ID {id} не найден");
        }

        if (seatCategoryUpdate.PriceCents != null)
        {


            seatCategory.PriceCents = seatCategoryUpdate.PriceCents.Value;
        }

        if (!string.IsNullOrEmpty(seatCategoryUpdate.Name))
        {
            seatCategory.Name = seatCategoryUpdate.Name;
        }

        await _context.SaveChangesAsync();

        return seatCategory;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var seatCategory = await _context.SeatCategories.FindAsync(id);

        if (seatCategory == null)
        {
            throw new KeyNotFoundException($"Категория с ID {id} не найденa");
        }

        _context.SeatCategories.Remove(seatCategory);
        var result = await _context.SaveChangesAsync();

        return result > 0;
    }




    public async Task<List<SeatCategory>> GetCategoriesBySeatIdAsync<T>(List<T> seats, Func<T, Guid> categoryIdSelector)
    {
        if (seats == null || !seats.Any())
        {
            return new List<SeatCategory>();
        }

        var categoryIds = seats.Select(categoryIdSelector).Distinct().ToList();

        var categories = await _context.SeatCategories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync();

        var foundCategoryIds = categories.Select(c => c.Id).ToList();
        var missingCategoryIds = categoryIds.Except(foundCategoryIds).ToList();

        if (missingCategoryIds.Any())
        {
            throw new KeyNotFoundException(
                $"Категории с ID {string.Join(", ", missingCategoryIds)} не найдены");
        }

        return categories;
    }
}