using Microsoft.EntityFrameworkCore;

namespace Yet_Another_Timesaver.Backend;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
}
