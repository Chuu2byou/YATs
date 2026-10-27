using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class Calendar : ComponentBase
{
    RadzenScheduler<TimeEntry> scheduler;
    EventConsole? console { get; set; }
    Dictionary<DateTime, string> events = new Dictionary<DateTime, string>();
    RadzenDayView? dayView;
    RadzenWeekView? weekView;
    RadzenMonthView? monthView;

    //default View == 2 for Calendar
    int selectedView = 2;

    async Task OnViewChange(int index)
    {
        selectedView = index;
        var view = index switch { 0 => (ISchedulerView)dayView, 1 => weekView, _ => monthView };
        await scheduler.SelectView(view);
    }


    bool showHeader = true;

    //Test Data
    IList<TimeEntry> TimeEntries = new List<TimeEntry>
    {
        new() { StartTime = DateTime.Today.AddDays(-2), EndTime = DateTime.Today.AddDays(-2), Description = "Birthday", Id = 1},
        new() { StartTime = DateTime.Today.AddDays(-11), EndTime = DateTime.Today.AddDays(-10), Description = "Day off", Id = 2},
        new() { StartTime = DateTime.Today.AddDays(-10), EndTime = DateTime.Today.AddDays(-8), Description = "Work from home", Id = 3},
        new() { StartTime = DateTime.Today.AddHours(10), EndTime = DateTime.Today.AddHours(12), Description = "Online meeting", Id = 4},
        new() { StartTime = DateTime.Today.AddHours(10), EndTime = DateTime.Today.AddHours(13), Description = "Skype call", Id = 5},
        new() { StartTime = DateTime.Today.AddHours(14), EndTime = DateTime.Today.AddHours(14).AddMinutes(30), Description = "Dentist appointment", Id = 6},
        new() { StartTime = DateTime.Today.AddHours(15), EndTime = DateTime.Today.AddHours(16), Description = "Team standup", Id = 7},
        new() { StartTime = DateTime.Today.AddHours(16), EndTime = DateTime.Today.AddHours(17), Description = "Code review", Id = 8},
        new() { StartTime = DateTime.Today.AddDays(1), EndTime = DateTime.Today.AddDays(12), Description = "Vacation", Id = 9 },
    };



    void OnDaySelect(SchedulerDaySelectEventArgs args)
    {
        console.Log($"DaySelect: Day={args.Day} TimeEntryCount={args.Appointments?.Count() ?? 0}");
    }

    void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        // Highlight today in month view
        if (args.View.Text == "Month" && args.Start.Date == DateTime.Today)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        // Highlight working hours (9-18)
        if ((args.View.Text == "Week" || args.View.Text == "Day") && args.Start.Hour > 8 && args.Start.Hour < 19)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }
    }

    async Task CreateTimeBooking()
    {
        console.Log($"Creating Timebooking");

        TimeEntry timeBooking = await DialogService.OpenAsync<AddTimeEntryPage>("Add TimeEntry",
            new Dictionary<string, object> { { "Start", DateTime.Now }, { "End", DateTime.Now.AddMinutes(30) } });

        if (timeBooking != null)
        {
            console.Log($"Timebooking created: Start={timeBooking.StartTime} Ende={timeBooking.EndTime}");
            TimeEntries.Add(timeBooking);
            await scheduler.Reload();
        }
    }

    async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        console?.Log($"SlotSelect: Start={args.Start} End={args.End}");

        if (args.View?.Text != "Year")
        {
            TimeEntry data = await DialogService.OpenAsync<AddTimeEntryPage>("Add TimeEntry",
                new Dictionary<string, object> { { "Start", args.Start }, { "End", args.End } });

            if (data != null)
            {
                TimeEntries.Add(data);
                // Either call the Reload method or reassign the Data property of the Scheduler
                await scheduler.Reload();
            }
        }
    }

    async Task OnTimeEntrySelect(SchedulerAppointmentSelectEventArgs<TimeEntry> args)
    {
        console?.Log($"TimeEntrySelect: TimeEntry={args.Data?.Description}");

        var copy = new TimeEntry
        {
            StartTime = args.Data.StartTime,
            EndTime = args.Data.EndTime,
            Description = args.Data.Description
        };

        var data = await DialogService.OpenAsync<EditTimeEntryPage>("Edit TimeEntry", new Dictionary<string, object> { { "TimeEntry", copy } });

        if (data != null)
        {
            // Update the time entry
            args.Data.StartTime = data.StartTime;
            args.Data.EndTime = data.EndTime;
            args.Data.Description = data.Description;
        }

        await scheduler.Reload();
    }

    void OnTimeEntryRender(SchedulerAppointmentRenderEventArgs<TimeEntry> args)
    {
        // Never call StateHasChanged in AppointmentRender - would lead to infinite loop

        if (args.Data.Description == "Birthday")
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

            if (args.SlotDate.TimeOfDay == TimeSpan.Zero)
            {
                draggedTimeEntry.StartTime = args.SlotDate.Date.Add(draggedTimeEntry.StartTime.TimeOfDay);
            }
            else
            {
                draggedTimeEntry.StartTime = args.SlotDate;
            }

            draggedTimeEntry.EndTime = draggedTimeEntry.StartTime.Add((TimeSpan)duration);

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

            console?.Log($"TimeEntryResize: TimeEntry={resizedTimeEntry.Description} Start={args.Start} End={args.End}");

            await scheduler.Reload();
        }
    }
}
