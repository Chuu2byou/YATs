using Microsoft.EntityFrameworkCore;

namespace Yet_Another_Timesaver.Backend;

public class TimeService
{
    private readonly AppDbContext _db;

    public TimeService(AppDbContext db)
    {
        _db = db;
    }

    // Select *
    public async Task<List<TimeEntry>> GetAllAsync()
    {
        return await _db.TimeEntries.ToListAsync();
    }

    // Insert into
    public async Task<TimeEntry> AddAsync(TimeEntry entry)
    {
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }
}
