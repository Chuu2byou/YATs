namespace Yet_Another_Timesaver.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public string User { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
}
