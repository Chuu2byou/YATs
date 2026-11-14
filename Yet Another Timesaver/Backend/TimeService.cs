using Microsoft.EntityFrameworkCore;

namespace Yet_Another_Timesaver.Backend;

public class TimeService
{
    private readonly AppDbContext _db;

    public TimeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TimeEntry>> GetAllAsync()
    {
        return await _db.TimeEntries.AsNoTracking().ToListAsync();
    }

    public async Task<TimeEntry?> GetByIdAsync(int id)
    {
        return await _db.TimeEntries.FindAsync(id);
    }

    public async Task<TimeEntry> AddAsync(TimeEntry entry)
    {
        ValidateTimeRange(entry);

        entry.Id = 0;
        entry.StartTime = ToUtc(entry.StartTime);
        entry.EndTime = entry.EndTime?.ToUniversalTime();
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> UpdateAsync(int id, TimeEntry entry)
    {
        ValidateTimeRange(entry);

        var savedEntry = await _db.TimeEntries.FindAsync(id);
        if (savedEntry is null)
        {
            return false;
        }

        savedEntry.Description = entry.Description;
        savedEntry.StartTime = ToUtc(entry.StartTime);
        savedEntry.EndTime = entry.EndTime?.ToUniversalTime();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entry = await _db.TimeEntries.FindAsync(id);
        if (entry is null)
        {
            return false;
        }

        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }

    // Throws if the entry's end time is not strictly after its start time.
    // An unset (null) end time is allowed and skips the check, since it
    // represents an ongoing, not-yet-finished booking.
    private static void ValidateTimeRange(TimeEntry entry)
    {
        if (entry.EndTime.HasValue && entry.EndTime.Value <= entry.StartTime)
        {
            throw new InvalidTimeRangeException("The end time must be after the start time.");
        }
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
