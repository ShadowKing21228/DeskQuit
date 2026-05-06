using System.Text.Json.Serialization;
using DeskQuit.Services.Notification;

namespace DeskQuit.Models;

public class ReminderConfig
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("IntervalInMinutes")]
    public int IntervalInMinutes { get; set; }
    
    [JsonPropertyName("NotificationStyle")]
    public NotificationStyle NotificationStyle { get; set; }

    [JsonPropertyName("IsCustom")]
    public bool IsCustom { get; set; }

    [JsonPropertyName("CustomTitle")]
    public string? CustomTitle { get; set; }

    [JsonPropertyName("CustomDescription")]
    public string? CustomDescription { get; set; }
}
