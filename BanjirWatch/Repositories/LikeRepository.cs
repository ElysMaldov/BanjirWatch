using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Models;
using BanjirWatch.Repositories.Interfaces;

namespace BanjirWatch.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly ApplicationDbContext _context;

    public LikeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Like?> GetByUserAndPostAsync(int userId, int postId)
    {
        return await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
    }

    public async Task<int> GetLikesCountAsync(int postId)
    {
        return await _context.Likes.CountAsync(l => l.PostId == postId);
    }

    public async Task<bool> ExistsAsync(int userId, int postId)
    {
        return await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId);
    }

    public async Task AddAsync(Like like)
    {
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Like like)
    {
        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();
    }
}
