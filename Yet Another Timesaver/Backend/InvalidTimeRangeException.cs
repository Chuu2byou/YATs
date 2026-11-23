namespace Yet_Another_Timesaver.Backend;

/// <summary>Represents an invalid time interval.</summary>
public class InvalidTimeRangeException : Exception
{
    public InvalidTimeRangeException(string message) : base(message)
    {
    }
}
