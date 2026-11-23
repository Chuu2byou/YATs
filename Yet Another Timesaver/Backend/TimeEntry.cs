namespace Yet_Another_Timesaver.Backend;

/// <summary>Represents a recorded time interval.</summary>
public class TimeEntry
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
