using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeskQuitServer.Models;

[Table("user_reminder")]
public class UserReminder
{
    [Key]
    [Column("id")]
    public required string Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }

    [Column("interval_in_minutes")]
    public int IntervalInMinutes { get; set; }

    [Column("notification_style")]
    public int NotificationStyle { get; set; }

    [Column("is_custom")]
    public bool IsCustom { get; set; }

    [Column("custom_title")]
    public string? CustomTitle { get; set; }

    [Column("custom_description")]
    public string? CustomDescription { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
