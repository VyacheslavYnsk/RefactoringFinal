public interface IHallService
{
    Task<Hall> CreateAsync(HallCreate hallCreate);
    Task<PaginationResponse<Hall>> GetListAsync(int page, int size);

    Task<Hall> GetByIdAsync(Guid id);

    Task<Hall> EditAsync(HallUpdate hallUpdate, Guid id);

    Task<bool> DeleteAsync(Guid id);

    Task<int> GetHallsRowsAsync(Guid id);

    Task<int> EditRowsCountAsync(int newRows, Guid id);



}