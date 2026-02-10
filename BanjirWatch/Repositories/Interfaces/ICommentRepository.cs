using BanjirWatch.Models;

namespace BanjirWatch.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id);
    Task AddAsync(Comment comment);
    Task DeleteAsync(Comment comment);
}
