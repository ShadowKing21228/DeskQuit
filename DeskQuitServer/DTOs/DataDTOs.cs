using System.ComponentModel.DataAnnotations;

namespace DeskQuitServer.DTOs;

// --- UserConfig DTOs ---
public class UserConfigDto
{
    [Required]
    public int? AfkThresholdMinutes { get; set; }
    [Required]
    public double? TimerWidth { get; set; }
    [Required]
    public double? TimerHeight { get; set; }
    [Required]
    public bool? RunOnStartup { get; set; }
}


// --- UserReminder DTOs ---
public class UserReminderDto
{
    [Required]
    public required string Id { get; set; }
    [Required]
    public bool? IsEnabled { get; set; }
    [Required]
    public int? IntervalInMinutes { get; set; }
    [Required]
    public int? NotificationStyle { get; set; }
    [Required]
    public bool? IsCustom { get; set; }
    public string? CustomTitle { get; set; }
    public string? CustomDescription { get; set; }
}


// --- UserStats DTOs ---
public class UserDailyStatsDto
{
    [Required]
    public DateOnly? StatDate { get; set; }
    [Required]
    public long? ActiveSeconds { get; set; }
    [Required]
    public long? AfkSeconds { get; set; }
    [Required]
    public int? NotificationsTotal { get; set; }
    [Required]
    public int? NotificationsCustom { get; set; }
}

public class UserDailyReminderStatsDto
{
    [Required]
    public DateOnly? StatDate { get; set; }
    [Required]
    public required string ReminderId { get; set; }
    [Required]
    public int? NotificationsCount { get; set; }
}
