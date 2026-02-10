using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BanjirWatch.Models.ViewModels;
using BanjirWatch.Services.Interfaces;
using BanjirWatch.Exceptions;

namespace BanjirWatch.Controllers;

public class PostsController : Controller
{
    private readonly IPostService _postService;
    private readonly ILogger<PostsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public PostsController(
        IPostService postService, 
        ILogger<PostsController> logger, 
        IWebHostEnvironment environment)
    {
        _postService = postService;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var currentUserId = GetCurrentUserId();

        var posts = await _postService.GetPostsAsync(page, pageSize, currentUserId);
        var totalPosts = await _postService.GetTotalPostsCountAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        ViewBag.HasMore = page < ViewBag.TotalPages;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_PostsList", posts);
        }

        return View(posts);
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

        var request = new CreatePostRequest
        {
            UserId = userId.Value,
            Content = model.Content,
            ImagePath = imagePath,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            LocationName = model.LocationName,
            IsFloodReport = model.IsFloodReport,
            Severity = model.Severity
        };

        await _postService.CreatePostAsync(request);

        _logger.LogInformation("User {UserId} created a post", userId.Value);

        TempData["SuccessMessage"] = "Post created successfully!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentUserId();
        var post = await _postService.GetPostByIdAsync(id, currentUserId);

        if (post == null)
        {
            return NotFound();
        }

        return View(post);
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

        try
        {
            await _postService.DeletePostAsync(id, userId.Value);
            _logger.LogInformation("User {UserId} deleted post {PostId}", userId.Value, id);
            TempData["SuccessMessage"] = "Post deleted successfully";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedException)
        {
            return Forbid();
        }

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

        var result = await _postService.ToggleLikeAsync(postId, userId.Value);
        return Json(new { liked = result.liked, likesCount = result.likesCount });
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

        try
        {
            var comment = await _postService.AddCommentAsync(postId, userId.Value, content);
            return Json(new
            {
                id = comment.Id,
                content = comment.Content,
                createdAt = comment.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                userId = comment.UserId,
                username = comment.Username,
                avatarPath = comment.AvatarPath
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            await _postService.DeleteCommentAsync(commentId, userId.Value);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedException)
        {
            return Forbid();
        }
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
