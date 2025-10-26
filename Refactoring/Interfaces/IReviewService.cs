public interface IReviewService
{
    Task<(IEnumerable<Review> Reviews, int TotalCount)> GetByFilmAsync(Guid filmId, int page, int size);
    Task<Review?> GetByIdAsync(Guid id);
    Task<Review> CreateAsync(Guid filmId, Guid clientId, ReviewCreate dto);
    Task<Review?> UpdateAsync(Guid id, Guid clientId, ReviewUpdate dto);
    Task<bool> DeleteAsync(Guid id, Guid clientId);
}
