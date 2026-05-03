using DeskQuit.Services.Notification;

namespace DeskQuit.Models;

public class NotificationStyleOption
{
    public NotificationStyle Style { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
