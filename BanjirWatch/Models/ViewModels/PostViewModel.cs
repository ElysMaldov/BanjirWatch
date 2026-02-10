namespace BanjirWatch.Models.ViewModels;

public class PostViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public bool IsFloodReport { get; set; }
    public FloodSeverity? Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // User info
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    
    // Engagement
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    
    // Comments
    public List<CommentViewModel> Comments { get; set; } = new();
}

public class CommentViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
}
