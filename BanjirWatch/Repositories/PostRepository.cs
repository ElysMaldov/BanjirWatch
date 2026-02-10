using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Models;
using BanjirWatch.Repositories.Interfaces;

namespace BanjirWatch.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ApplicationDbContext _context;

    public PostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Post>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Post>> GetFloodReportsAsync(int limit = 100)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Where(p => p.Latitude != null && p.Longitude != null)
            .Where(p => p.IsFloodReport)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Post>> GetRecentFloodReportsAsync(int limit = 6)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Where(p => p.IsFloodReport)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Post?> GetByIdAsync(int id)
    {
        return await _context.Posts.FindAsync(id);
    }

    public async Task<Post?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Posts.CountAsync();
    }

    public async Task<int> GetFloodReportsCountAsync()
    {
        return await _context.Posts.CountAsync(p => p.IsFloodReport);
    }

    public async Task AddAsync(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Post post)
    {
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Post post)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Posts.AnyAsync(p => p.Id == id);
    }
}
