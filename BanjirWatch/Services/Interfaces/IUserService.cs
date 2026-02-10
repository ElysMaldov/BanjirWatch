using BanjirWatch.Models;

namespace BanjirWatch.Services.Interfaces;

/// <summary>
/// Service for managing users and authentication-related operations
/// </summary>
public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdWithPostsAsync(int id);
    Task<User?> AuthenticateAsync(string usernameOrEmail, string password);
    Task<(User user, bool success, string error)> RegisterAsync(string username, string email, string password);
    Task UpdateLastLoginAsync(int userId);
    Task<int> GetTotalUsersCountAsync();
    Task<string?> UpdateAvatarAsync(int userId, Stream fileStream, string fileName, string contentType);
    Task DeleteAvatarAsync(int userId);
}
