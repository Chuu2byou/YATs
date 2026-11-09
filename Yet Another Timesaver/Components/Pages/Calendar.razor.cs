using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class Calendar : ComponentBase
{
    [Inject]
    private TimeService TimeService { get; set; } = default!;

    RadzenScheduler<TimeEntry> scheduler = default!;
    RadzenDayView? dayView;
    RadzenWeekView? weekView;
    RadzenMonthView? monthView;

    //default View == 2 for Calendar
    int selectedView = 2;

    async Task OnViewChange(int index)
    {
        selectedView = index;
        ISchedulerView? view = index switch { 0 => dayView, 1 => weekView, _ => monthView };
        if (view is not null)
        {
            await scheduler.SelectView(view);
        }
    }

    IList<TimeEntry> TimeEntries = new List<TimeEntry>();

    protected override async Task OnInitializedAsync()
    {
        TimeEntries = (await TimeService.GetAllAsync())
            .Select(ToLocal)
            .ToList();
    }

    void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        // Highlight today in month view
        if (args.View?.Text == "Month" && args.Start.Date == TimeService.Today)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        // Highlight working hours (9-18)
        if ((args.View?.Text == "Week" || args.View?.Text == "Day") && args.Start.Hour > 8 && args.Start.Hour < 19)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }
    }

    async Task CreateTimeBooking()
    {
        var timeBooking = await DialogService.OpenAsync<AddTimeEntryPage>("Add TimeEntry",
            new Dictionary<string, object?> { { "Start", TimeService.Now }, { "End", TimeService.Now.AddMinutes(30) } });

        if (timeBooking is TimeEntry entry)
        {
            TimeEntries.Add(ToLocal(await TimeService.AddAsync(entry)));
            await scheduler.Reload();
        }
    }

    async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        //console?.Log($"SlotSelect: Start={args.Start} End={args.End}");

        if (args.View?.Text != "Year")
        {
            var data = await DialogService.OpenAsync<AddTimeEntryPage>("Add TimeEntry",
                new Dictionary<string, object?> { { "Start", args.Start }, { "End", args.End } });

            if (data is TimeEntry entry)
            {
                TimeEntries.Add(ToLocal(await TimeService.AddAsync(entry)));
                await scheduler.Reload();
            }
        }
    }

    async Task OnTimeEntrySelect(SchedulerAppointmentSelectEventArgs<TimeEntry> args)
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

        var data = await DialogService.OpenAsync<EditTimeEntryPage>("Edit TimeEntry", new Dictionary<string, object?> { { "TimeEntry", copy } });

        if (data is TimeEntry updatedEntry)
        {
            await SaveExistingEntryAsync(updatedEntry);

            selectedEntry.StartTime = updatedEntry.StartTime;
            selectedEntry.EndTime = updatedEntry.EndTime;
            selectedEntry.Description = updatedEntry.Description;
        }

        await scheduler.Reload();
    }

    void OnTimeEntryRender(SchedulerAppointmentRenderEventArgs<TimeEntry> args)
    {
        // Never call StateHasChanged in AppointmentRender - would lead to infinite loop

        if (args.Data?.Description == "Birthday")
        {
            args.Attributes["style"] = "background: red";
        }
    }

    async Task OnTimeEntryMove(SchedulerAppointmentMoveEventArgs args)
    {
        var draggedTimeEntry = TimeEntries.FirstOrDefault(x => x == args?.Appointment?.Data);

        if (draggedTimeEntry != null)
        {
            var duration = draggedTimeEntry.EndTime - draggedTimeEntry.StartTime;
            if (duration is null)
            {
                return;
            }

            if (args.SlotDate.TimeOfDay == TimeSpan.Zero)
            {
                draggedTimeEntry.StartTime = args.SlotDate.Date.Add(draggedTimeEntry.StartTime.TimeOfDay);
            }
            else
            {
                draggedTimeEntry.StartTime = args.SlotDate;
            }

            draggedTimeEntry.EndTime = draggedTimeEntry.StartTime.Add(duration.Value);

            await SaveExistingEntryAsync(draggedTimeEntry);
            await scheduler.Reload();
        }
    }

    async Task OnTimeEntryResize(SchedulerAppointmentResizeEventArgs args)
    {
        var resizedTimeEntry = TimeEntries.FirstOrDefault(x => x == args?.Appointment?.Data);

        if (resizedTimeEntry != null)
        {
            resizedTimeEntry.StartTime = args.Start;
            resizedTimeEntry.EndTime = args.End;

            await SaveExistingEntryAsync(resizedTimeEntry);
            await scheduler.Reload();
        }
    }

    private async Task SaveExistingEntryAsync(TimeEntry entry)
    {
        if (!await TimeService.UpdateAsync(entry.Id, entry))
        {
            throw new InvalidOperationException($"Time entry {entry.Id} no longer exists.");
        }
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
