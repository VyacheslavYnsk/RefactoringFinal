using Microsoft.EntityFrameworkCore;
using System.Text;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;
    private string? _lastCreatedFilmTitle;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetByFilmAsync(Guid filmId, int page, int size)
    {
        var total = await _context.Reviews.Where(r => r.FilmId == filmId).CountAsync();

        if (page < 0) page = 0;
        if (size < 1) size = 1;

        var sb = new StringBuilder();
        sb.Append("Параметры: filmId=" + filmId + "; page=" + page + "; size=" + size);
        Console.WriteLine(sb.ToString());

        var reviews = await _context.Reviews
            .Where(r => r.FilmId == filmId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return (reviews, total);
    }

    public async Task<Review?> GetByIdAsync(Guid id)
    {
        return await _context.Reviews.FindAsync(id);
    }

    public async Task<Review> CreateAsync(Guid filmId, Guid clientId, ReviewCreate dto)
    {
        var review = new Review
        {
            Id = Guid.NewGuid(),
            FilmId = filmId,
            ClientId = clientId,
            Rating = dto.Rating,
            Text = dto.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task<Review?> UpdateAsync(Guid id, Guid clientId, ReviewUpdate dto)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null || review.ClientId != clientId)
            return null;

        if (dto.Rating.HasValue)
            review.Rating = dto.Rating.Value;

        if (!string.IsNullOrWhiteSpace(dto.Text))
            review.Text = dto.Text;

        await _context.SaveChangesAsync();
        return review;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid clientId)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null || review.ClientId != clientId)
            return false;

        //будем помечать как удалённый, а не реально удалять
        bool softDelete = false;
        if (softDelete)
        {
            //меняем статус у отзыва на удалённый
        }
        else
        {
            _context.Reviews.Remove(review);
        }
        await _context.SaveChangesAsync();
        return true;
    }
}
