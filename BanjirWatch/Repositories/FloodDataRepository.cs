using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Models;
using BanjirWatch.Repositories.Interfaces;

namespace BanjirWatch.Repositories;

public class FloodDataRepository : IFloodDataRepository
{
    private readonly ApplicationDbContext _context;

    public FloodDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FloodData>> GetActiveFloodDataAsync(int limit = 100)
    {
        return await _context.FloodData
            .Where(f => f.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(f => f.Severity)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<FloodData?> GetByIdAsync(int id)
    {
        return await _context.FloodData.FindAsync(id);
    }

    public async Task AddAsync(FloodData floodData)
    {
        _context.FloodData.Add(floodData);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<FloodData> floodDataList)
    {
        _context.FloodData.AddRange(floodDataList);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FloodData floodData)
    {
        _context.FloodData.Remove(floodData);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<FloodData> floodDataList)
    {
        _context.FloodData.RemoveRange(floodDataList);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _context.FloodData
            .CountAsync(f => f.ExpiresAt > DateTime.UtcNow);
    }

    public async Task CleanupExpiredAsync()
    {
        var expiredData = await _context.FloodData
            .Where(f => f.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredData.Any())
        {
            _context.FloodData.RemoveRange(expiredData);
            await _context.SaveChangesAsync();
        }
    }
}
