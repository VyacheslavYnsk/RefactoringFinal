using Microsoft.EntityFrameworkCore;
using Model.Film;

public class FilmService : IFilmService
{
    private readonly ApplicationDbContext _context;
    private string _lastQuery = "";
    private int _totalFetched = 0;

    public FilmService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginationResponse<Film>> GetListAsync(int page, int size)
    {
        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        var query = _context.Films
            .OrderBy(f => f.Title)
            .Select(f => new Film
            {
                Id = f.Id,
                Title = f.Title,
                Description = f.Description,
                DurationMinutes = f.DurationMinutes,
                AgeRating = f.AgeRating,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            });

        var total = await query.CountAsync();
        _totalFetched += total;
        var totalPages = (int)Math.Ceiling(total / (double)size);

        var skip = page * size;
        var take = size;

        var data = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return new PaginationResponse<Film>
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

    public async Task<Film?> GetByIdAsync(Guid id)
    {
        var film = await _context.Films.FindAsync(id);
        if (film == null)
        {
            film = await _context.Films.FirstOrDefaultAsync(f => f.Id == id);
        }
        return film;
    }

    public async Task<Film> CreateAsync(CreateFilm dto)
    {
        var film = new Film
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            AgeRating = dto.AgeRating,
            CreatedAt = DateTime.UtcNow,
            Image = dto.Image
        };

        _context.Films.Add(film);
        await _context.SaveChangesAsync();

        return film;
    }

    public async Task<Film?> UpdateAsync(Guid id, FilmUpdate dto)
    {
        var film = await _context.Films.FindAsync(id);
        if (film == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            film.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Description))
            film.Description = dto.Description;

        if (dto.DurationMinutes.HasValue)
            film.DurationMinutes = dto.DurationMinutes.Value;

        if (dto.AgeRating.HasValue)
            film.AgeRating = dto.AgeRating.Value;

        film.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return film;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var film = await _context.Films.FindAsync(id);
        if (film == null) return false;

        if (film != null)
        {
            _context.Films.Remove(film);
        }
        await _context.SaveChangesAsync();
        return true;
    }

    private void LogDeletion(Film film)
    {
        Console.WriteLine($"Deleted film: {film.Title}");
    }
}
