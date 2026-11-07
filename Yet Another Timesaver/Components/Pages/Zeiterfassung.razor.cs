using Microsoft.AspNetCore.Components;
using Yet_Another_Timesaver.Backend;
namespace Yet_Another_Timesaver.Components.Pages

{
    public partial class Zeiterfassung
    {
        [Inject]
        private TimeService TimeService { get; set; } = default!;

        private TimeEntry? currentTimeBooking;
        private bool IsWorking = false;

        private string StartTime = "--:--";
        private string EndTime = "--:--";

        private DateTime SelectedDate = DateTime.Today;

        private string TodayHours => FormatHours(Entries.Where(entry => entry.StartTime.Date == DateTime.Today));

        private string WeekHours
        {
            get
            {
                var today = DateTime.Today;
                var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                return FormatHours(Entries.Where(entry =>
                    entry.StartTime.Date >= monday && entry.StartTime.Date <= today));
            }
        }

        private string WorkStatus =>
            IsWorking ? "Aktiv" : "Nicht aktiv";

        private string StatusDescription =>
            IsWorking
                ? "Deine Arbeitszeit läuft."
                : "Noch keine aktive Buchung.";

        private string CurrentDate =>
            DateTime.Today.ToString("dddd, dd. MMMM yyyy");

        private List<TimeEntry> Entries = new();

        protected override async Task OnInitializedAsync()
        {
            Entries = (await TimeService.GetAllAsync())
                .Select(ToLocalTime)
                .ToList();
            UpdateDisplayedBooking();
        }

        private async Task StartWork()
        {
            var savedEntry = await TimeService.AddAsync(new TimeEntry
            {
                Description = "Arbeitszeit",
                StartTime = DateTime.Now
            });
            currentTimeBooking = ToLocalTime(savedEntry);
            Entries.Add(currentTimeBooking);
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

            currentTimeBooking.EndTime = DateTime.Now;
            if (!await TimeService.UpdateAsync(currentTimeBooking.Id, currentTimeBooking))
            {
                throw new InvalidOperationException($"Time entry {currentTimeBooking.Id} no longer exists.");
            }

            EndTime = currentTimeBooking.EndTime.Value.ToString("T");
            IsWorking = false;
            currentTimeBooking = null;
        }

        private async Task DeleteEntry(TimeEntry entry)
        {
            if (!await TimeService.DeleteAsync(entry.Id))
            {
                throw new InvalidOperationException($"Time entry {entry.Id} no longer exists.");
            }

            Entries.Remove(entry);
            UpdateDisplayedBooking();
        }

        private async Task RefreshData()
        {
            Entries = (await TimeService.GetAllAsync())
                .Select(ToLocalTime)
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
                .Where(entry => entry.StartTime.Date == DateTime.Today)
                .OrderByDescending(entry => entry.StartTime)
                .FirstOrDefault();
            StartTime = latestEntry?.StartTime.ToString("T") ?? "--:--";
            EndTime = latestEntry?.EndTime?.ToString("T") ?? "--:--";
        }

        private static TimeEntry ToLocalTime(TimeEntry entry)
        {
            return new TimeEntry
            {
                Id = entry.Id,
                Description = entry.Description,
                StartTime = entry.StartTime.ToLocalTime(),
                EndTime = entry.EndTime?.ToLocalTime()
            };
        }

        private static string FormatHours(IEnumerable<TimeEntry> entries)
        {
            var duration = entries.Aggregate(
                TimeSpan.Zero,
                (total, entry) => total + ((entry.EndTime ?? DateTime.Now) - entry.StartTime));
            return $"{(int)duration.TotalHours} Std. {duration.Minutes:00} Min.";
        }
    }
}
