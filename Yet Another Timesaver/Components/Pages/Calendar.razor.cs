using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

/// <summary>Displays and manages time entries in a calendar.</summary>
public partial class Calendar : ComponentBase
{
    [Inject] private TimeService TimeService { get; set; } = default!;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private RadzenScheduler<TimeEntry> scheduler = default!;
    private RadzenDayView? dayView;
    private RadzenWeekView? weekView;
    private RadzenMonthView? monthView;
    private IList<TimeEntry> TimeEntries = new List<TimeEntry>();
    private WeekViewSettings weekViewSettings = WeekViewSettings.Default;
    private CultureInfo weekViewCulture = CreateCulture(WeekViewSettings.Default.StartDay);
    private readonly TimeSpan dayStart = TimeSpan.Zero;
    private readonly TimeSpan dayEnd = new TimeSpan(23, 59, 59);

    private int selectedView = 1;

    private const int DayColumnOffset = 2;

    private int SaturdayColumnIndex =>
        ((int)DayOfWeek.Saturday - (int)weekViewSettings.StartDay + 7) % 7 + DayColumnOffset;

    private int SundayColumnIndex =>
        ((int)DayOfWeek.Sunday - (int)weekViewSettings.StartDay + 7) % 7 + DayColumnOffset;

    private string WeekendHideClass =>
        weekViewSettings.WorkWeekOnly && selectedView == 1
            ? $"wd-hide-{SaturdayColumnIndex} wd-hide-{SundayColumnIndex}"
            : string.Empty;

    private string SchedulerCssClass => $"app-page app-page--full {WeekendHideClass}";

    private bool viewReady;

    private string SchedulerVisibilityStyle => viewReady ? "" : "visibility: hidden;";

    protected override async Task OnInitializedAsync()
    {
        TimeEntries = (await TimeService.GetAllAsync())
            .Select(ToLocal)
            .ToList();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            weekViewSettings = await LoadWeekViewSettingsAsync();
            weekViewCulture = CreateCulture(weekViewSettings.StartDay);

            if (weekView is not null)
            {
                await scheduler.SelectView(weekView);
            }

            await scheduler.Reload();

            viewReady = true;
            StateHasChanged();
        }
    }

    private static CultureInfo CreateCulture(DayOfWeek startDay)
    {
        var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        culture.DateTimeFormat.FirstDayOfWeek = startDay;
        return culture;
    }

    private async Task OnViewChange(int index)
    {
        selectedView = index;
        ISchedulerView? view = index switch { 0 => dayView, 1 => weekView, _ => monthView };
        if (view is not null)
        {
            await scheduler.SelectView(view);
            await scheduler.Reload();
        }
    }

    private async Task OpenWeekViewSettingsAsync()
    {
        var result = await DialogService.OpenAsync<WeekViewSettingsDialog>("Week View Settings",
            new Dictionary<string, object?> { { "Settings", weekViewSettings } });

        if (result is WeekViewSettings updated)
        {
            weekViewSettings = updated;
            weekViewCulture = CreateCulture(weekViewSettings.StartDay);
            await SaveWeekViewSettingsAsync();
            await scheduler.Reload();
        }
    }

    private async Task<WeekViewSettings> LoadWeekViewSettingsAsync()
    {
        try
        {
            var json = await JSRuntime.InvokeAsync<string?>("calendarSettings.load");
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonSerializer.Deserialize<WeekViewSettings>(json);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch (JSException)
        {
        }

        return WeekViewSettings.Default;
    }

    private async Task SaveWeekViewSettingsAsync()
    {
        var json = JsonSerializer.Serialize(weekViewSettings);
        await JSRuntime.InvokeVoidAsync("calendarSettings.save", json);
    }

    private void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        if (args.View?.Text == "Month" && args.Start.Date == TimeService.Today)
        {
            args.Attributes["style"] =
                "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        if ((args.View?.Text == "Week" || args.View?.Text == "Day") && args.Start.Hour > 8 && args.Start.Hour < 19)
        {
            args.Attributes["style"] =
                "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }
    }

    private async Task CreateTimeBooking()
    {
        var timeBooking = await DialogService.OpenAsync<AddTimeEntryPage>("Add TimeEntry",
            new Dictionary<string, object?>
                { { "Start", TimeService.Now }, { "End", TimeService.Now.AddMinutes(30) } });

        if (timeBooking is TimeEntry entry)
        {
            await TryAddEntryAsync(entry);
        }
    }

    private async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        if (args.View?.Text != "Year")
        {
            var data = await DialogService.OpenAsync<AddTimeEntryPage>("Add TimeEntry",
                new Dictionary<string, object?> { { "Start", args.Start }, { "End", args.End } });

            if (data is TimeEntry entry)
            {
                await TryAddEntryAsync(entry);
            }
        }
    }

    private async Task OnTimeEntrySelect(SchedulerAppointmentSelectEventArgs<TimeEntry> args)
    {
        if (args.Data is not { } selectedEntry)
        {
            return;
        }

        var copy = new TimeEntry
        {
            Id = selectedEntry.Id,
            StartTime = selectedEntry.StartTime,
            EndTime = selectedEntry.EndTime,
            Description = selectedEntry.Description
        };

        var data = await DialogService.OpenAsync<EditTimeEntryPage>("Edit TimeEntry",
            new Dictionary<string, object?> { { "TimeEntry", copy } });

        switch (data)
        {
            case TimeEntryDeleteRequest deleteRequest:
                await TryDeleteEntryAsync(deleteRequest.Id, selectedEntry);
                break;

            case TimeEntry updatedEntry:
                if (await TrySaveExistingEntryAsync(updatedEntry))
                {
                    selectedEntry.StartTime = updatedEntry.StartTime;
                    selectedEntry.EndTime = updatedEntry.EndTime;
                    selectedEntry.Description = updatedEntry.Description;
                }

                break;
        }

        await scheduler.Reload();
    }

    private void OnTimeEntryRender(SchedulerAppointmentRenderEventArgs<TimeEntry> args)
    {
        if (args.Data?.Description == "Birthday")
            args.Attributes["style"] = "background: red";
        if (args.Data?.Description == "Vacation")
            args.Attributes["style"] = "background: green";
        if (args.Data?.Description == "Sick Leave")
            args.Attributes["style"] = "background: pink";
    }

    private async Task OnTimeEntryMove(SchedulerAppointmentMoveEventArgs args)
    {
        var draggedTimeEntry = TimeEntries.FirstOrDefault(x => x == args.Appointment?.Data);

        if (draggedTimeEntry is null)
        {
            return;
        }

        var duration = draggedTimeEntry.EndTime - draggedTimeEntry.StartTime;
        if (duration is null)
        {
            return;
        }

        var newStart = args.SlotDate.TimeOfDay == TimeSpan.Zero
            ? args.SlotDate.Date.Add(draggedTimeEntry.StartTime.TimeOfDay)
            : args.SlotDate;

        var originalStart = draggedTimeEntry.StartTime;
        var originalEnd = draggedTimeEntry.EndTime;

        draggedTimeEntry.StartTime = newStart;
        draggedTimeEntry.EndTime = newStart.Add(duration.Value);

        if (!await TrySaveExistingEntryAsync(draggedTimeEntry))
        {
            draggedTimeEntry.StartTime = originalStart;
            draggedTimeEntry.EndTime = originalEnd;
        }

        await scheduler.Reload();
    }

    private async Task OnTimeEntryResize(SchedulerAppointmentResizeEventArgs args)
    {
        var resizedTimeEntry = TimeEntries.FirstOrDefault(x => x == args.Appointment?.Data);

        if (resizedTimeEntry is null)
        {
            return;
        }

        if (args.End <= args.Start)
        {
            ShowError("The end time must be after the start time.");
            await scheduler.Reload();
            return;
        }

        var originalStart = resizedTimeEntry.StartTime;
        var originalEnd = resizedTimeEntry.EndTime;

        resizedTimeEntry.StartTime = args.Start;
        resizedTimeEntry.EndTime = args.End;

        if (!await TrySaveExistingEntryAsync(resizedTimeEntry))
        {
            resizedTimeEntry.StartTime = originalStart;
            resizedTimeEntry.EndTime = originalEnd;
        }

        await scheduler.Reload();
    }

    private async Task TryAddEntryAsync(TimeEntry entry)
    {
        try
        {
            TimeEntries.Add(ToLocal(await TimeService.AddAsync(entry)));
            await scheduler.Reload();
        }
        catch (InvalidTimeRangeException ex)
        {
            ShowError(ex.Message);
        }
    }

    private async Task<bool> TrySaveExistingEntryAsync(TimeEntry entry)
    {
        try
        {
            if (!await TimeService.UpdateAsync(entry.Id, entry))
            {
                ShowError("This time entry no longer exists.");
                return false;
            }

            return true;
        }
        catch (InvalidTimeRangeException ex)
        {
            ShowError(ex.Message);
            return false;
        }
    }

    private async Task TryDeleteEntryAsync(int id, TimeEntry selectedEntry)
    {
        if (await TimeService.DeleteAsync(id))
        {
            TimeEntries.Remove(selectedEntry);
        }
        else
        {
            ShowError("This time entry no longer exists.");
        }
    }

    private void ShowError(string message)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = "Booking error",
            Detail = message,
            Duration = 4000
        });
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
}
