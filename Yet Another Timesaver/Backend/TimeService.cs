using Microsoft.EntityFrameworkCore;

namespace Yet_Another_Timesaver.Backend;

/// <summary>Manages time entries and time zone conversions.</summary>
public class TimeService
{
    private static readonly TimeZoneInfo AppZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly AppDbContext _db;

    public TimeService(AppDbContext db)
    {
        _db = db;
    }

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppZone);

    public DateTime Today => Now.Date;

    public DateTime ToLocal(DateTime utcTime) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), AppZone);

    public DateTime? ToLocal(DateTime? utcTime)
    {
        if (utcTime == null)
        {
            return null;
        }

        return ToLocal(utcTime.Value);
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
        entry.EndTime = ToUtc(entry.EndTime);
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
        savedEntry.EndTime = ToUtc(entry.EndTime);
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

    private static void ValidateTimeRange(TimeEntry entry)
    {
        if (entry.EndTime.HasValue && entry.EndTime.Value <= entry.StartTime)
        {
            throw new InvalidTimeRangeException("The end time must be after the start time.");
        }
    }

    private static DateTime ToUtc(DateTime localTime) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), AppZone);

    private static DateTime? ToUtc(DateTime? localTime)
    {
        if (localTime == null)
        {
            return null;
        }

        return ToUtc(localTime.Value);
    }
}
