using BanjirWatch.Exceptions;
using BanjirWatch.Models;
using BanjirWatch.Models.ViewModels;
using BanjirWatch.Repositories.Interfaces;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Services;

/// <summary>
/// Service for managing posts and their related operations
/// </summary>
public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly ICommentRepository _commentRepository;

    public PostService(
        IPostRepository postRepository,
        ILikeRepository likeRepository,
        ICommentRepository commentRepository)
    {
        _postRepository = postRepository;
        _likeRepository = likeRepository;
        _commentRepository = commentRepository;
    }

    public async Task<List<PostViewModel>> GetPostsAsync(int page = 1, int pageSize = 10, int? currentUserId = null)
    {
        var posts = await _postRepository.GetAllAsync(page, pageSize);
        return posts.Select(p => MapToViewModel(p, currentUserId)).ToList();
    }

    public async Task<PostViewModel?> GetPostByIdAsync(int id, int? currentUserId = null)
    {
        var post = await _postRepository.GetByIdWithDetailsAsync(id);
        return post == null ? null : MapToViewModel(post, currentUserId);
    }

    public async Task<Post> CreatePostAsync(CreatePostRequest request)
    {
        var post = new Post
        {
            UserId = request.UserId,
            Content = request.Content,
            ImagePath = request.ImagePath,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationName = request.LocationName,
            IsFloodReport = request.IsFloodReport,
            Severity = request.Severity,
            CreatedAt = DateTime.UtcNow
        };

        await _postRepository.AddAsync(post);
        return post;
    }

    public async Task DeletePostAsync(int postId, int userId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            throw new NotFoundException($"Post {postId} not found");

        if (post.UserId != userId)
            throw new UnauthorizedException("You can only delete your own posts");

        await _postRepository.DeleteAsync(post);
    }

    public async Task<(bool liked, int likesCount)> ToggleLikeAsync(int postId, int userId)
    {
        var existingLike = await _likeRepository.GetByUserAndPostAsync(userId, postId);

        if (existingLike != null)
        {
            await _likeRepository.DeleteAsync(existingLike);
            var countAfterUnlike = await _likeRepository.GetLikesCountAsync(postId);
            return (false, countAfterUnlike);
        }
        else
        {
            var like = new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _likeRepository.AddAsync(like);
            var countAfterLike = await _likeRepository.GetLikesCountAsync(postId);
            return (true, countAfterLike);
        }
    }

    public async Task<CommentViewModel> AddCommentAsync(int postId, int userId, string content)
    {
        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment);

        // Reload to get user info
        var post = await _postRepository.GetByIdWithDetailsAsync(postId);
        var createdComment = post?.Comments.FirstOrDefault(c => c.Id == comment.Id);

        if (createdComment == null)
            throw new InvalidOperationException("Failed to create comment");

        return new CommentViewModel
        {
            Id = createdComment.Id,
            Content = createdComment.Content,
            CreatedAt = createdComment.CreatedAt,
            UserId = createdComment.UserId,
            Username = createdComment.User.Username,
            AvatarPath = createdComment.User.AvatarPath
        };
    }

    public async Task DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            throw new NotFoundException($"Comment {commentId} not found");

        if (comment.UserId != userId)
            throw new UnauthorizedException("You can only delete your own comments");

        await _commentRepository.DeleteAsync(comment);
    }

    public async Task<int> GetTotalPostsCountAsync()
    {
        return await _postRepository.GetTotalCountAsync();
    }

    public async Task<List<PostViewModel>> GetRecentFloodReportsAsync(int limit = 6, int? currentUserId = null)
    {
        var posts = await _postRepository.GetRecentFloodReportsAsync(limit);
        return posts.Select(p => MapToViewModel(p, currentUserId)).ToList();
    }

    public async Task<List<PostViewModel>> GetFloodReportsForMapAsync(int limit = 100)
    {
        var posts = await _postRepository.GetFloodReportsAsync(limit);
        return posts.Select(p => new PostViewModel
        {
            Id = p.Id,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            Content = p.Content,
            ImagePath = p.ImagePath,
            LocationName = p.LocationName,
            CreatedAt = p.CreatedAt,
            Username = p.User.Username,
            AvatarPath = p.User.AvatarPath,
            LikesCount = p.Likes.Count,
            CommentsCount = p.Comments.Count
        }).ToList();
    }

    private PostViewModel MapToViewModel(Post post, int? currentUserId)
    {
        return new PostViewModel
        {
            Id = post.Id,
            Content = post.Content,
            ImagePath = post.ImagePath,
            Latitude = post.Latitude,
            Longitude = post.Longitude,
            LocationName = post.LocationName,
            IsFloodReport = post.IsFloodReport,
            Severity = post.Severity,
            CreatedAt = post.CreatedAt,
            UserId = post.UserId,
            Username = post.User.Username,
            AvatarPath = post.User.AvatarPath,
            LikesCount = post.Likes.Count,
            CommentsCount = post.Comments.Count,
            IsLikedByCurrentUser = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value),
            Comments = post.Comments.OrderBy(c => c.CreatedAt).Select(c => new CommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UserId = c.UserId,
                Username = c.User.Username,
                AvatarPath = c.User.AvatarPath
            }).ToList()
        };
    }
}
