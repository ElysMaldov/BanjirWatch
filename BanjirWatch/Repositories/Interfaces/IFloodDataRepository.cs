using BanjirWatch.Models;

namespace BanjirWatch.Repositories.Interfaces;

public interface IFloodDataRepository
{
    Task<List<FloodData>> GetActiveFloodDataAsync(int limit = 100);
    Task<FloodData?> GetByIdAsync(int id);
    Task AddAsync(FloodData floodData);
    Task AddRangeAsync(IEnumerable<FloodData> floodDataList);
    Task DeleteAsync(FloodData floodData);
    Task DeleteRangeAsync(IEnumerable<FloodData> floodDataList);
    Task<int> GetActiveCountAsync();
    Task CleanupExpiredAsync();
}
