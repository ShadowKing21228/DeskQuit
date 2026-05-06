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
}
