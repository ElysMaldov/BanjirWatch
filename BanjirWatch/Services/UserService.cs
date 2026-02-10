using BanjirWatch.Models;
using BanjirWatch.Repositories.Interfaces;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Services;

/// <summary>
/// Service for managing users and authentication-related operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IWebHostEnvironment environment,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _environment = environment;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User?> GetByIdWithPostsAsync(int id)
    {
        return await _userRepository.GetByIdWithPostsAsync(id);
    }

    public async Task<User?> AuthenticateAsync(string usernameOrEmail, string password)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<(User user, bool success, string error)> RegisterAsync(string username, string email, string password)
    {
        // Check if username exists
        if (await _userRepository.UsernameExistsAsync(username))
        {
            return (null!, false, "Username already exists");
        }

        // Check if email exists
        if (await _userRepository.EmailExistsAsync(email))
        {
            return (null!, false, "Email already exists");
        }

        // Create user
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        _logger.LogInformation("New user registered: {Username}", user.Username);

        return (user, true, string.Empty);
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
        return await _userRepository.GetTotalCountAsync();
    }

    public async Task<string?> UpdateAvatarAsync(int userId, Stream fileStream, string fileName, string contentType)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            await DeleteAvatarAsync(userId);
        }

        // Save new avatar
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        var newFileName = $"{user.Id}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(uploadsFolder, newFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        var avatarPath = $"/uploads/avatars/{newFileName}";
        user.AvatarPath = avatarPath;
        await _userRepository.UpdateAsync(user);

        return avatarPath;
    }

    public async Task DeleteAvatarAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.AvatarPath))
            return;

        var oldPath = Path.Combine(_environment.WebRootPath, user.AvatarPath.TrimStart('/'));
        if (File.Exists(oldPath))
        {
            File.Delete(oldPath);
        }

        user.AvatarPath = null;
        await _userRepository.UpdateAsync(user);
    }
}
