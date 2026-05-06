using System.ComponentModel.DataAnnotations.Schema;

namespace DeskQuitServer.Models;

[Table("user_daily_stats")]
public class UserDailyStats
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("stat_date")]
    public DateOnly StatDate { get; set; }

    [Column("active_seconds")]
    public long ActiveSeconds { get; set; }

    [Column("afk_seconds")]
    public long AfkSeconds { get; set; }

    [Column("notifications_total")]
    public int NotificationsTotal { get; set; }

    [Column("notifications_custom")]
    public int NotificationsCustom { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
