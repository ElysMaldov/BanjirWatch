using BanjirWatch.Models;
using BanjirWatch.Models.ViewModels;

namespace BanjirWatch.Services.Interfaces;

/// <summary>
/// Service for managing posts and their related operations
/// </summary>
public interface IPostService
{
    Task<List<PostViewModel>> GetPostsAsync(int page = 1, int pageSize = 10, int? currentUserId = null);
    Task<PostViewModel?> GetPostByIdAsync(int id, int? currentUserId = null);
    Task<Post> CreatePostAsync(CreatePostRequest request);
    Task DeletePostAsync(int postId, int userId);
    Task<(bool liked, int likesCount)> ToggleLikeAsync(int postId, int userId);
    Task<CommentViewModel> AddCommentAsync(int postId, int userId, string content);
    Task DeleteCommentAsync(int commentId, int userId);
    Task<int> GetTotalPostsCountAsync();
    Task<List<PostViewModel>> GetRecentFloodReportsAsync(int limit = 6, int? currentUserId = null);
    Task<List<PostViewModel>> GetFloodReportsForMapAsync(int limit = 100);
}

public class CreatePostRequest
{
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public bool IsFloodReport { get; set; } = true;
    public FloodSeverity? Severity { get; set; }
}
