using Microsoft.EntityFrameworkCore;

namespace Yet_Another_Timesaver.Backend;

/// <summary>Provides the application's Entity Framework database context.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
}
