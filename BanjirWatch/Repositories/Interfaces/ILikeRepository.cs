using BanjirWatch.Models;

namespace BanjirWatch.Repositories.Interfaces;

public interface ILikeRepository
{
    Task<Like?> GetByUserAndPostAsync(int userId, int postId);
    Task<int> GetLikesCountAsync(int postId);
    Task<bool> ExistsAsync(int userId, int postId);
    Task AddAsync(Like like);
    Task DeleteAsync(Like like);
}
