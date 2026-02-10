using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Models;
using BanjirWatch.Models.ViewModels;

namespace BanjirWatch.Controllers;

public class PostsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public PostsController(ApplicationDbContext context, ILogger<PostsController> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var currentUserId = GetCurrentUserId();

        var postsQuery = _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var posts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var postViewModels = posts.Select(p => new PostViewModel
        {
            Id = p.Id,
            Content = p.Content,
            ImagePath = p.ImagePath,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            LocationName = p.LocationName,
            IsFloodReport = p.IsFloodReport,
            Severity = p.Severity,
            CreatedAt = p.CreatedAt,
            UserId = p.UserId,
            Username = p.User.Username,
            AvatarPath = p.User.AvatarPath,
            LikesCount = p.Likes.Count,
            CommentsCount = p.Comments.Count,
            IsLikedByCurrentUser = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value),
            Comments = p.Comments.OrderBy(c => c.CreatedAt).Select(c => new CommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UserId = c.UserId,
                Username = c.User.Username,
                AvatarPath = c.User.AvatarPath
            }).ToList()
        }).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        ViewBag.HasMore = page < ViewBag.TotalPages;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_PostsList", postViewModels);
        }

        return View(postViewModels);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreatePostViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePostViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        string? imagePath = null;

        // Handle image upload
        if (model.Image != null && model.Image.Length > 0)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(model.Image.ContentType))
            {
                ModelState.AddModelError(nameof(model.Image), "Only image files (JPG, PNG, GIF, WebP) are allowed");
                return View(model);
            }

            if (model.Image.Length > 10 * 1024 * 1024) // 10MB limit
            {
                ModelState.AddModelError(nameof(model.Image), "File size must be less than 10MB");
                return View(model);
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "posts");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{userId.Value}_{Guid.NewGuid():N}{Path.GetExtension(model.Image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.Image.CopyToAsync(stream);
            }

            imagePath = $"/uploads/posts/{fileName}";
        }

        var post = new Post
        {
            UserId = userId.Value,
            Content = model.Content,
            ImagePath = imagePath,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            LocationName = model.LocationName,
            IsFloodReport = model.IsFloodReport,
            Severity = model.Severity,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created post {PostId}", userId.Value, post.Id);

        TempData["SuccessMessage"] = "Post created successfully!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentUserId();

        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        var viewModel = new PostViewModel
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

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        // Only allow deletion by post owner or admin (simplified - just owner for now)
        if (post.UserId != userId.Value)
        {
            return Forbid();
        }

        // Delete associated image
        if (!string.IsNullOrEmpty(post.ImagePath))
        {
            var imagePath = Path.Combine(_environment.WebRootPath, post.ImagePath.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted post {PostId}", userId.Value, id);

        TempData["SuccessMessage"] = "Post deleted successfully";
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ToggleLike(int postId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId.Value);

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return Json(new { liked = false, likesCount = await _context.Likes.CountAsync(l => l.PostId == postId) });
        }
        else
        {
            var like = new Like
            {
                PostId = postId,
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return Json(new { liked = true, likesCount = await _context.Likes.CountAsync(l => l.PostId == postId) });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment(int postId, string content)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest("Content is required");
        }

        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
        {
            return NotFound();
        }

        var comment = new Comment
        {
            PostId = postId,
            UserId = userId.Value,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Load user data for response
        await _context.Entry(comment).Reference(c => c.User).LoadAsync();

        return Json(new
        {
            id = comment.Id,
            content = comment.Content,
            createdAt = comment.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
            userId = comment.UserId,
            username = comment.User.Username,
            avatarPath = comment.User.AvatarPath
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return NotFound();
        }

        if (comment.UserId != userId.Value)
        {
            return Forbid();
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return Ok();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
