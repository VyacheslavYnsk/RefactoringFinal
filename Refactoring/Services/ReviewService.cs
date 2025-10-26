using Microsoft.EntityFrameworkCore;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetByFilmAsync(Guid filmId, int page, int size)
    {
        var query = _context.Reviews.Where(r => r.FilmId == filmId);
        var total = await query.CountAsync();

        var reviews = await query
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

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }
}
