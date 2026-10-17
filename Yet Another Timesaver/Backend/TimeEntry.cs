namespace Yet_Another_Timesaver.Backend;

public class TimeEntry
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
}
