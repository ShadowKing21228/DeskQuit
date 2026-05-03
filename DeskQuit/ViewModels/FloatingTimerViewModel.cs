using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DeskQuit.Services.Notification;

namespace DeskQuit.ViewModels;

public partial class FloatingTimerViewModel : ViewModelBase
{
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private string _formattedTime = "00:00:00";

    public FloatingTimerViewModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
        _notificationService.TotalTimeChanged += OnTotalTimeChanged;
        
        // Initial value
        UpdateFormattedTime(_notificationService.TotalWorkTime);
    }

    private void OnTotalTimeChanged(TimeSpan time)
    {
        // Must update UI on the UI thread
        Dispatcher.UIThread.Post(() => UpdateFormattedTime(time));
    }

    private void UpdateFormattedTime(TimeSpan time)
    {
        FormattedTime = $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
