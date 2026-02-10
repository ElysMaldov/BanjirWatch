using BanjirWatch.Data;
using BanjirWatch.Models;
using BanjirWatch.Repositories.Interfaces;

namespace BanjirWatch.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        return await _context.Comments.FindAsync(id);
    }

    public async Task AddAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Comment comment)
    {
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
    }
}
