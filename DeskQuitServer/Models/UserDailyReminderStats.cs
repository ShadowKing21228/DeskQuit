using System.ComponentModel.DataAnnotations.Schema;

namespace DeskQuitServer.Models;

[Table("user_daily_reminder_stats")]
public class UserDailyReminderStats
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("stat_date")]
    public DateOnly StatDate { get; set; }

    [Column("reminder_id")]
    public required string ReminderId { get; set; }

    [Column("notifications_count")]
    public int NotificationsCount { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
