using DeskQuitServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskQuitServer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<UserConfig> UserConfigs { get; set; }
    public DbSet<UserReminder> UserReminders { get; set; }
    public DbSet<UserDailyStats> UserDailyStats { get; set; }
    public DbSet<UserDailyReminderStats> UserDailyReminderStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite primary keys
        modelBuilder.Entity<UserDailyStats>()
            .HasKey(e => new { e.UserId, e.StatDate });

        modelBuilder.Entity<UserDailyReminderStats>()
            .HasKey(e => new { e.UserId, e.StatDate, e.ReminderId });
    }
}
