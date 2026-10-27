using Yet_Another_Timesaver.Backend;
namespace Yet_Another_Timesaver.Components.Pages

{
    public partial class Zeiterfassung
    {
        private TimeEntry currentTimeBooking;
        private bool IsWorking = false;

        private string StartTime = "--:--";
        private string EndTime = "--:--";

        private DateTime SelectedDate = DateTime.Today;

        private string TodayHours = "4 Std. 03 Min.";
        private string WeekHours = "12 Std. 33 Min.";

        private string WorkStatus =>
            IsWorking ? "Aktiv" : "Nicht aktiv";

        private string StatusDescription =>
            IsWorking
                ? "Deine Arbeitszeit läuft."
                : "Noch keine aktive Buchung.";

        private string CurrentDate =>
            DateTime.Today.ToString("dddd, dd. MMMM yyyy");

        //Test Data
        private List<TimeEntry> Entries = new()
    {
        new TimeEntry
        {
            StartTime = DateTime.Parse("08:15"),
            EndTime = DateTime.Parse("12:00")
        },

        new TimeEntry
        {
            StartTime = DateTime.Parse("08:15"),
            EndTime = DateTime.Parse("16:30"),
        },

        new TimeEntry
        {
            StartTime = DateTime.Parse("09:00"),
            EndTime = DateTime.Parse("17:00"),
        }
    };

        private void StartWork()
        {
            IsWorking = true;
            currentTimeBooking = new TimeEntry
            {
                StartTime = DateTime.Now,
                Id = Entries.Count + 1
            };
            StartTime = currentTimeBooking.StartTime.ToString("T");
            RefreshData();
        }

        private void StopWork()
        {
            IsWorking = false;
            currentTimeBooking.EndTime = DateTime.Now;
            EndTime = currentTimeBooking.EndTime?.ToString("T");
            Entries.Add(currentTimeBooking);  
            currentTimeBooking = null;
            StateHasChanged();
        }

        private void DeleteEntry(TimeEntry entry)
        {
            Entries.Remove(entry);
        }

        private void RefreshData()
        {
            StateHasChanged();
        }

    }
}
