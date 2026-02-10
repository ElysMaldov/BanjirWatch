using BanjirWatch.Models;

namespace BanjirWatch.Repositories.Interfaces;

public interface IPostRepository
{
    Task<List<Post>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<List<Post>> GetFloodReportsAsync(int limit = 100);
    Task<List<Post>> GetRecentFloodReportsAsync(int limit = 6);
    Task<Post?> GetByIdAsync(int id);
    Task<Post?> GetByIdWithDetailsAsync(int id);
    Task<int> GetTotalCountAsync();
    Task<int> GetFloodReportsCountAsync();
    Task AddAsync(Post post);
    Task UpdateAsync(Post post);
    Task DeleteAsync(Post post);
    Task<bool> ExistsAsync(int id);
}
