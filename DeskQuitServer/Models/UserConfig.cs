using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeskQuitServer.Models;

[Table("user_config")]
public class UserConfig
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("afk_threshold_minutes")]
    public int AfkThresholdMinutes { get; set; }

    [Column("timer_width")]
    public double TimerWidth { get; set; }

    [Column("timer_height")]
    public double TimerHeight { get; set; }

    [Column("run_on_startup")]
    public bool RunOnStartup { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
