using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DeskQuit.Services.Localization;

namespace DeskQuit.ViewModels.Notifications;

public class AggressiveBlockingNotificationViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherTimer _unlockTimer;

    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(10);
    
    private TimeSpan _left;
    
    private string _actionButtonText = string.Empty;
    
    private bool _isActionEnabled;

    public string Title { get; }
    
    public string Body { get; }
    
    public string ActionButtonText
    {
        get => _actionButtonText;
        private set => SetProperty(ref _actionButtonText, value);
    }

    public bool IsActionEnabled
    {
        get => _isActionEnabled;
        private set => SetProperty(ref _isActionEnabled, value);
    }

    public bool CanClose { get; private set; }
    
    public IRelayCommand StartBreakCommand { get; }

    public event Action? BreakStartedRequested;

    public AggressiveBlockingNotificationViewModel(string title, string body)
    {
        Title = title;
        Body = body;
        _localizationService.LanguageChanged += OnLanguageChanged;

        _left = _cooldown;
        _unlockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _unlockTimer.Tick += OnUnlockTick;

        StartBreakCommand = new RelayCommand(OnStartBreak, () => IsActionEnabled);
        UpdateButtonText();
    }

    public AggressiveBlockingNotificationViewModel() : this("Test", "Test body")
    {
        
    }

    public void StartCooldown()
    {
        if (IsActionEnabled)
            return;

        _unlockTimer.Start();
    }

    public void StopCooldown()
    {
        _unlockTimer.Stop();
    }

    private void OnUnlockTick(object? sender, EventArgs e)
    {
        _left -= TimeSpan.FromSeconds(1);
        if (_left > TimeSpan.Zero)
        {
            UpdateButtonText();
            return;
        }

        _unlockTimer.Stop();
        IsActionEnabled = true;
        ActionButtonText = _localizationService["notification.aggressive.start.now"];
        StartBreakCommand.NotifyCanExecuteChanged();
    }

    private void OnStartBreak()
    {
        if (!IsActionEnabled)
            return;

        CanClose = true;
        BreakStartedRequested?.Invoke();
    }

    private void UpdateButtonText()
    {
        ActionButtonText = _localizationService.Format("notification.aggressive.start.in", (int)Math.Ceiling(_left.TotalSeconds));
    }

    private void OnLanguageChanged(AppLanguage _)
    {
        if (IsActionEnabled)
        {
            ActionButtonText = _localizationService["notification.aggressive.start.now"];
            return;
        }

        UpdateButtonText();
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }
}
