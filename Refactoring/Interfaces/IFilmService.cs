using Model.Film;

public interface IFilmService
{
    Task<PaginationResponse<Film>> GetListAsync(int page, int size);
    Task<Film?> GetByIdAsync(Guid id);
    Task<Film> CreateAsync(CreateFilm dto);
    Task<Film?> UpdateAsync(Guid id, FilmUpdate dto);
    Task<bool> DeleteAsync(Guid id);
}
