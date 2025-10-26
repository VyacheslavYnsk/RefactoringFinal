public interface ISessionService
{
    Task<(IEnumerable<Session> Sessions, int TotalCount)> GetAllAsync(int page, int size, Guid? filmId, DateTime? date);
    Task<Session?> GetByIdAsync(Guid id);
    Task<Session> CreateAsync(SessionCreate dto);
    Task<Session?> UpdateAsync(Guid id, SessionUpdate dto);
    Task<bool> DeleteAsync(Guid id);
}
