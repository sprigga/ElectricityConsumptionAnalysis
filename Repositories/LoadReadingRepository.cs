using Microsoft.EntityFrameworkCore;
using PowerAnalysis.Data;
using PowerAnalysis.Models;

namespace PowerAnalysis.Repositories;

/// <summary>
/// 負載讀數 Repository 實作
/// </summary>
public class LoadReadingRepository : ILoadReadingRepository
{
    private readonly ApplicationDbContext _context;

    public LoadReadingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LoadReading>> GetAllAsync()
    {
        return await _context.LoadReadings
            .OrderBy(lr => lr.Timestamp)
            .ToListAsync();
    }

    public async Task<LoadReading?> GetByIdAsync(int id)
    {
        return await _context.LoadReadings.FindAsync(id);
    }

    public async Task<IEnumerable<LoadReading>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.LoadReadings
            .Where(lr => lr.Timestamp >= startDate && lr.Timestamp <= endDate)
            .OrderBy(lr => lr.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoadReading>> GetByDataSourceAsync(string dataSource)
    {
        return await _context.LoadReadings
            .Where(lr => lr.DataSource == dataSource)
            .OrderBy(lr => lr.Timestamp)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(DateTime timestamp)
    {
        return await _context.LoadReadings
            .AnyAsync(lr => lr.Timestamp == timestamp);
    }

    public async Task AddAsync(LoadReading entity)
    {
        await _context.LoadReadings.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<LoadReading> entities)
    {
        await _context.LoadReadings.AddRangeAsync(entities);
    }

    public void Update(LoadReading entity)
    {
        _context.LoadReadings.Update(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.LoadReadings.FindAsync(id);
        if (entity != null)
        {
            _context.LoadReadings.Remove(entity);
        }
    }

    public async Task DeleteByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var entities = await _context.LoadReadings
            .Where(lr => lr.Timestamp >= startDate && lr.Timestamp <= endDate)
            .ToListAsync();

        _context.LoadReadings.RemoveRange(entities);
    }

    public async Task<int> CountAsync()
    {
        return await _context.LoadReadings.CountAsync();
    }

    public async Task<(DateTime? MinDate, DateTime? MaxDate)> GetDateRangeAsync()
    {
        var hasData = await _context.LoadReadings.AnyAsync();
        if (!hasData)
        {
            return (null, null);
        }

        var minDate = await _context.LoadReadings.MinAsync(lr => lr.Timestamp);
        var maxDate = await _context.LoadReadings.MaxAsync(lr => lr.Timestamp);

        return (minDate, maxDate);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
