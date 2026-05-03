using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using DeskQuit.Services.Localization;
using DeskQuit.Services.Logging;
using DeskQuit.Services.Notification;
using DeskQuit.Views;

namespace DeskQuit.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NotificationService _notificationService;
    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    
    // Ссылка на плавающее окно
    private FloatingTimerWindow? _floatingTimerWindow;
    
    public string Greeting => _localizationService["main.greeting"];
    
    public MainWindowViewModel(NotificationService notificationService)
    {
        AppLogger.Info("Constructor initialized", nameof(MainWindowViewModel));
        _notificationService = notificationService;
        _localizationService.LanguageChanged += _ => OnPropertyChanged(nameof(Greeting));
        AddTasks();
    }

    private void AddTasks()
    {
        AppLogger.Info("Adding tasks", nameof(MainWindowViewModel));
        var task = new NotificationTask(
            _localizationService["notification.soft.title"],
            _localizationService["notification.soft.body"],
            new TimeSpan(0, 0, 5),
            NotificationStyle.SoftPersistentTelegram,
            "notification.soft.title",
            "notification.soft.body");

        var task2 = new NotificationTask(
            _localizationService["notification.aggressive.title"],
            _localizationService["notification.aggressive.body"],
            new TimeSpan(0, 0, 30),
            NotificationStyle.AggressiveBlocking,
            "notification.aggressive.title",
            "notification.aggressive.body");
        
        List<NotificationTask> list = [task, task2];
        _notificationService.AddTasks(list);
    }
    
    [RelayCommand]
    private void ToggleFloatingTimer()
    {
        if (_floatingTimerWindow == null)
        {
            _floatingTimerWindow = new FloatingTimerWindow
            {
                DataContext = new FloatingTimerViewModel(_notificationService)
            };
            
            // Если окно закрыли извне, сбрасываем ссылку
            _floatingTimerWindow.Closed += (s, e) => _floatingTimerWindow = null;
            _floatingTimerWindow.Show();
        }
        else
        {
            _floatingTimerWindow.Close();
            _floatingTimerWindow = null;
        }
    }
}
