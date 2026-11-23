namespace Yet_Another_Timesaver.Backend;

/// <summary>Stores calendar week view preferences.</summary>
public class WeekViewSettings
{
    public DayOfWeek StartDay { get; set; } = DayOfWeek.Monday;
    public bool WorkWeekOnly { get; set; }

    public static WeekViewSettings Default => new();
}
