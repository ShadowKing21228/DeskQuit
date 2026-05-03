using System;
using System.Collections.Generic;
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

    // Порог бездействия (после которого считаем пользователя AFK и ставим таймеры на паузу)
    // Например, 1 минута.
    private readonly TimeSpan _idleThreshold = TimeSpan.FromSeconds(1);
    
    // Глобальное время работы программы (активное время за ПК)
    public TimeSpan TotalWorkTime { get; private set; } = TimeSpan.Zero;
        
    public event Action<TimeSpan>? TotalTimeChanged;

    
    public NotificationService()
    {
        _heartbeat = new DispatcherTimer 
        { 
            Interval = TimeSpan.FromSeconds(1) 
        };
        _heartbeat.Tick += OnHeartbeat;
    }

    public void Initialize()
    {
        AppLogger.Info("Initialize", nameof(NotificationService));
        // Выбираем менеджер в зависимости от ОС
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _manager = new WindowsNotificationManager();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Для Linux (стандарт FreeDesktop/dbus)
            _manager = new FreeDesktopNotificationManager();
        }
        
        // Инициализируем (это может быть async в зависимости от версии, 
        // проверь доступность Initialize или InitializeAsync)
        _manager?.Initialize();
        Start();
    }
    
    public async Task SendNotification(string title, string body)
    {
        if (_manager == null) return;

        var nf = new DesktopNotifications.Notification
        {
            Title = title,
            Body = body
        };
        AppLogger.Info("SendNotification", nameof(NotificationService));
        await _manager.ShowNotification(nf);
    }

    public void AddTask(NotificationTask task)
    {
        AppLogger.Info($"AddTask. HasElapsedAction={task.IsHaveElapsedAction}", nameof(NotificationService));
        
        if (!task.IsHaveElapsedAction) 
            AddDefaultAction(task);
        
        _tasks.Add(task);
    }

    public void AddTasks(IEnumerable<NotificationTask> tasks)
    {
        foreach (var task in tasks)
            AddTask(task);
    }

    public void Start() => _heartbeat.Start();

    private void AddDefaultAction(NotificationTask task)
    {
        AppLogger.Info("AddDefaultAction", nameof(NotificationService));
        task.Elapsed += async notificationTask =>
        {
            AppLogger.Info("Default action executed", nameof(NotificationService));
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

    private async Task ShowSoftPersistentNotification(NotificationTask task, string title, string body)
    {
        if (_softNotificationVisible)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_softNotificationVisible)
                return;

            _softNotificationVisible = true;
            var window = new SoftPersistentNotificationWindow(title, body);
            window.DoneClicked += () =>
            {
                task.TimeLeft = task.Interval;
                _softNotificationVisible = false;
            };
            window.SnoozeClicked += snooze =>
            {
                task.TimeLeft = snooze;
                _softNotificationVisible = false;
            };
            window.Closed += (_, _) => _softNotificationVisible = false;
            window.Show();
        });
    }

    private async Task ShowAggressiveBlockingNotification(NotificationTask task, string title, string body)
    {
        if (_aggressiveNotificationVisible)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_aggressiveNotificationVisible)
                return;

            _aggressiveNotificationVisible = true;
            var window = new AggressiveBlockingNotificationWindow(title, body);
            window.BreakStarted += () =>
            {
                task.TimeLeft = task.Interval;
                _aggressiveNotificationVisible = false;
            };
            window.Closed += (_, _) => _aggressiveNotificationVisible = false;
            window.Show();
        });
    }

    private void OnHeartbeat(object? sender, EventArgs e)
    {
        // Проверяем время бездействия (AFK)
        var idleTime = UserActivityService.GetIdleTime();
        
        // Если пользователь бездействует дольше порога (например, отошел от ПК), 
        // мы приостанавливаем отсчет времени.
        if (idleTime > _idleThreshold)
        {
            // Пользователь в AFK. Не обновляем задачи и общее время.
            return;
        }

        var second = TimeSpan.FromSeconds(1);
        
        // 1. Обновляем общее время
        TotalWorkTime += second;
        TotalTimeChanged?.Invoke(TotalWorkTime);

        // 2. Обновляем все независимые задачи
        foreach (var task in _tasks)
        {
            task.Update(second);
        }
    }
}
