using Microsoft.AspNetCore.Components;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

/// <summary>Displays and manages the time tracking dashboard.</summary>
public partial class TimeTracking
{
    [Inject] private TimeService TimeService { get; set; } = default!;

    private TimeEntry? currentTimeBooking;
    private bool IsWorking = false;

    private string StartTime = "--:--";
    private string EndTime = "--:--";

    private DateTime? SelectedDate;

    private string TodayHours => FormatHours(Entries.Where(entry => entry.StartTime.Date == TimeService.Today));

    private string WeekHours
    {
        get
        {
            var today = TimeService.Today;
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            return FormatHours(Entries.Where(entry =>
                entry.StartTime.Date >= monday && entry.StartTime.Date <= today));
        }
    }

    private string WorkStatus =>
        IsWorking ? "Active" : "Inactive";

    private string StatusDescription =>
        IsWorking
            ? "Your work session is in progress."
            : "No active work session.";

    private string CurrentDate =>
        TimeService.Today.ToString("dddd, dd. MMMM yyyy");

    private List<TimeEntry> Entries = new();

    private IEnumerable<TimeEntry> FilteredEntries =>
        SelectedDate is null
            ? Entries
            : Entries.Where(entry => entry.StartTime.Date == SelectedDate.Value.Date);

    protected override async Task OnInitializedAsync()
    {
        SelectedDate = TimeService.Today;
        Entries = (await TimeService.GetAllAsync())
            .Select(ToLocal)
            .ToList();
        UpdateDisplayedBooking();
    }

    private async Task StartWork()
    {
        var savedEntry = await TimeService.AddAsync(new TimeEntry
        {
            Description = "Work time",
            StartTime = TimeService.Now
        });
        currentTimeBooking = ToLocal(savedEntry);
        Entries.Add(currentTimeBooking);
        Entries = Entries.ToList();
        IsWorking = true;
        StartTime = currentTimeBooking.StartTime.ToString("T");
        EndTime = "--:--";
    }

    private async Task StopWork()
    {
        if (currentTimeBooking is null)
        {
            return;
        }

        currentTimeBooking.EndTime = TimeService.Now;
        if (!await TimeService.UpdateAsync(currentTimeBooking.Id, currentTimeBooking))
        {
            throw new InvalidOperationException($"Time entry {currentTimeBooking.Id} no longer exists.");
        }

        EndTime = currentTimeBooking.EndTime.Value.ToString("T");
        IsWorking = false;
        currentTimeBooking = null;
        Entries = Entries.ToList();
    }

    private async Task DeleteEntry(TimeEntry entry)
    {
        if (!await TimeService.DeleteAsync(entry.Id))
        {
            throw new InvalidOperationException($"Time entry {entry.Id} no longer exists.");
        }

        Entries.Remove(entry);
        Entries = Entries.ToList();
        UpdateDisplayedBooking();
    }

    private async Task RefreshData()
    {
        Entries = (await TimeService.GetAllAsync())
            .Select(ToLocal)
            .ToList();
        UpdateDisplayedBooking();
    }

    private void UpdateDisplayedBooking()
    {
        currentTimeBooking = Entries
            .Where(entry => entry.EndTime is null)
            .OrderByDescending(entry => entry.StartTime)
            .FirstOrDefault();
        IsWorking = currentTimeBooking is not null;

        if (currentTimeBooking is not null)
        {
            StartTime = currentTimeBooking.StartTime.ToString("T");
            EndTime = "--:--";
            return;
        }

        var latestEntry = Entries
            .Where(entry => entry.StartTime.Date == TimeService.Today)
            .OrderByDescending(entry => entry.StartTime)
            .FirstOrDefault();
        StartTime = latestEntry?.StartTime.ToString("T") ?? "--:--";
        EndTime = latestEntry?.EndTime?.ToString("T") ?? "--:--";
    }

    private TimeEntry ToLocal(TimeEntry entry)
    {
        return new TimeEntry
        {
            Id = entry.Id,
            Description = entry.Description,
            StartTime = TimeService.ToLocal(entry.StartTime),
            EndTime = TimeService.ToLocal(entry.EndTime)
        };
    }

    private string FormatHours(IEnumerable<TimeEntry> entries)
    {
        var duration = entries.Aggregate(
            TimeSpan.Zero,
            (total, entry) => total + ((entry.EndTime ?? TimeService.Now) - entry.StartTime));
        return $"{(int)duration.TotalHours} hrs {duration.Minutes:00} min.";
    }
}
