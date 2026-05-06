using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskQuit.Models;

public class GlobalConfig
{
    [JsonPropertyName("AfkThresholdMinutes")]
    public int AfkThresholdMinutes { get; set; } = 1;

    [JsonPropertyName("Reminders")]
    public List<ReminderConfig> Reminders { get; set; } = new();

    [JsonPropertyName("TimerWidth")]
    public double TimerWidth { get; set; } = 180;

    [JsonPropertyName("TimerHeight")]
    public double TimerHeight { get; set; } = 60;

    [JsonPropertyName("RunOnStartup")]
    public bool RunOnStartup { get; set; } = false;

    [JsonPropertyName("ServerUrl")]
    public string ServerUrl { get; set; } = "http://localhost:8080/api/";

    // Authentication info
    [JsonPropertyName("JwtToken")]
    public string? JwtToken { get; set; }

    [JsonPropertyName("UserEmail")]
    public string? UserEmail { get; set; }
}
