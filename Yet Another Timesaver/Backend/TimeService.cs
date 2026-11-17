using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Yet_Another_Timesaver.Backend;

public class TimeService
{
    private static readonly TimeZoneInfo AppZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly AppDbContext _db;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public TimeService(AppDbContext db, AuthenticationStateProvider authenticationStateProvider)
    {
        _db = db;
        _authenticationStateProvider = authenticationStateProvider;
    }

    private async Task<int> GetCurrentUserIdAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var id = state.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        return int.Parse(id);
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
        var userId = await GetCurrentUserIdAsync();
        return await _db.TimeEntries
            .Where(entry => entry.UserId == userId)
            .ToListAsync();
    }

    public async Task<TimeEntry?> GetByIdAsync(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        return await _db.TimeEntries
            .FirstOrDefaultAsync(entry => entry.Id == id && entry.UserId == userId);
    }

    public async Task<TimeEntry> AddAsync(TimeEntry entry)
    {
        entry.Id = 0;
        entry.UserId = await GetCurrentUserIdAsync();
        entry.StartTime = ToUtc(entry.StartTime);
        entry.EndTime = ToUtc(entry.EndTime);
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> UpdateAsync(int id, TimeEntry entry)
    {
        var userId = await GetCurrentUserIdAsync();
        var savedEntry = await _db.TimeEntries.FindAsync(id);
        if (savedEntry is null || savedEntry.UserId != userId)
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
        var userId = await GetCurrentUserIdAsync();
        var entry = await _db.TimeEntries.FindAsync(id);
        if (entry is null || entry.UserId != userId)
        {
            return false;
        }

        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
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
