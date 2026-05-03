using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using DesktopNotifications;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using DeskQuit.Services.Localization;
using DeskQuit.Services.Logging;
using DeskQuit.Views.Notifications;

namespace DeskQuit.Services.Notification;

public class NotificationService
{
    private readonly DispatcherTimer _heartbeat;
    private readonly List<NotificationTask> _tasks = [];
    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    private bool _softNotificationVisible;
    private bool _aggressiveNotificationVisible;
    private INotificationManager? _manager;
    private TimeSpan _idleThreshold = TimeSpan.FromMinutes(1);
    public TimeSpan TotalWorkTime { get; private set; } = TimeSpan.Zero;
    public event Action<TimeSpan>? TotalTimeChanged;

    public NotificationService()
    {
        _heartbeat = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _heartbeat.Tick += OnHeartbeat;
    }

    public void Initialize()
    {
        AppLogger.Info("Initialize", nameof(NotificationService));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _manager = new WindowsNotificationManager();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _manager = new FreeDesktopNotificationManager();
        
        _manager?.Initialize();
        Start();
    }
    
    public void SetAfkThreshold(TimeSpan threshold)
    {
        _idleThreshold = threshold;
    }

    public void AddTask(NotificationTask task)
    {
        AppLogger.Info($"AddTask. TitleKey={task.TitleKey}", nameof(NotificationService));
        if (!task.IsHaveElapsedAction)
            AddDefaultAction(task);
        _tasks.Add(task);
    }
    
    public void RemoveTask(NotificationTask task)
    {
        AppLogger.Info($"RemoveTask. TitleKey={task.TitleKey}", nameof(NotificationService));
        _tasks.Remove(task);
    }

    public NotificationTask? FindTask(string? titleKey)
    {
        if (string.IsNullOrEmpty(titleKey)) return null;
        return _tasks.FirstOrDefault(t => t.TitleKey == titleKey);
    }

    public void ClearTasks()
    {
        AppLogger.Info("Clearing all tasks", nameof(NotificationService));
        _tasks.Clear();
    }

    public void Start() => _heartbeat.Start();

    private void OnHeartbeat(object? sender, EventArgs e)
    {
        var idleTime = UserActivityService.GetIdleTime();
        if (idleTime > _idleThreshold) return;

        var second = TimeSpan.FromSeconds(1);
        TotalWorkTime += second;
        TotalTimeChanged?.Invoke(TotalWorkTime);

        foreach (var task in _tasks)
        {
            task.Update(second);
        }
    }
    
    private void AddDefaultAction(NotificationTask task)
    {
        task.Elapsed += async notificationTask =>
        {
            await SendNotificationByStyle(notificationTask);
        };
    }

    private Task SendNotificationByStyle(NotificationTask task)
    {
        var title = task.ResolveTitle(_localizationService);
        var body = task.ResolveText(_localizationService);

        return task.Style switch
        {
            NotificationStyle.SoftPersistentTelegram => ShowSoftPersistentNotification(task, title, body),
            NotificationStyle.AggressiveBlocking => ShowAggressiveBlockingNotification(task, title, body),
            _ => SendNotification(title, body)
        };
    }
    
    public async Task SendNotification(string title, string body)
    {
        if (_manager == null) return;
        var nf = new DesktopNotifications.Notification { Title = title, Body = body };
        await _manager.ShowNotification(nf);
    }

    private async Task ShowSoftPersistentNotification(NotificationTask task, string title, string body)
    {
        if (_softNotificationVisible) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_softNotificationVisible) return;
            _softNotificationVisible = true;
            var window = new SoftPersistentNotificationWindow(title, body);
            window.DoneClicked += () => { task.TimeLeft = task.Interval; _softNotificationVisible = false; };
            window.SnoozeClicked += snooze => { task.TimeLeft = snooze; _softNotificationVisible = false; };
            window.Closed += (_, _) => _softNotificationVisible = false;
            window.Show();
        });
    }

    private async Task ShowAggressiveBlockingNotification(NotificationTask task, string title, string body)
    {
        if (_aggressiveNotificationVisible) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_aggressiveNotificationVisible) return;
            _aggressiveNotificationVisible = true;
            var window = new AggressiveBlockingNotificationWindow(title, body);
            window.BreakStarted += () => { task.TimeLeft = task.Interval; _aggressiveNotificationVisible = false; };
            window.Closed += (_, _) => _aggressiveNotificationVisible = false;
            window.Show();
        });
    }
}
