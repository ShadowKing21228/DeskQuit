using System;
using DeskQuit.Services.Localization;
using CommunityToolkit.Mvvm.Input;

namespace DeskQuit.ViewModels.Notifications;

public class SoftPersistentNotificationViewModel : ViewModelBase, IDisposable
{
    private readonly LocalizationService _localizationService = LocalizationService.Instance;

    public string Title { get; }
    
    public string Body { get; }
    
    public bool CanClose { get; private set; }

    public string SnoozeButtonText => _localizationService["notification.soft.snooze"];
    
    public string DoneButtonText => _localizationService["notification.soft.done"];

    public IRelayCommand DoneCommand { get; }
    public IRelayCommand SnoozeCommand { get; }

    public event Action? DoneRequested;
    public event Action<TimeSpan>? SnoozeRequested;

    public SoftPersistentNotificationViewModel(string title, string body)
    {
        Title = title;
        Body = body;
        _localizationService.LanguageChanged += OnLanguageChanged;

        DoneCommand = new RelayCommand(OnDone);
        SnoozeCommand = new RelayCommand(OnSnooze);
    }
    
    public SoftPersistentNotificationViewModel() : this("Test", "Test body")
    {
        
    }

    private void OnDone()
    {
        CanClose = true;
        DoneRequested?.Invoke();
    }

    private void OnSnooze()
    {
        CanClose = true;
        SnoozeRequested?.Invoke(TimeSpan.FromMinutes(5));
    }

    private void OnLanguageChanged(AppLanguage _)
    {
        OnPropertyChanged(nameof(SnoozeButtonText));
        OnPropertyChanged(nameof(DoneButtonText));
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }
}
