using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskQuit.Models;
using DeskQuit.Services;
using DeskQuit.Services.Localization;
using DeskQuit.Services.Notification;

namespace DeskQuit.ViewModels;

public partial class AccountViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ConfigService _configService;
    private readonly NotificationService _notificationService;
    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    private readonly Action _onAuthStateChanged;

    [ObservableProperty]
    private string _emailInput = string.Empty;

    [ObservableProperty]
    private string _passwordInput = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _currentUserEmail = string.Empty;

    // Статистика текущей сессии
    [ObservableProperty]
    private string _currentSessionActiveTime = "0 ч. 0 мин.";
    
    [ObservableProperty]
    private string _currentSessionAfkTime = "0 ч. 0 мин.";
    
    [ObservableProperty]
    private int _currentSessionNotificationsFiredTotal;
    
    [ObservableProperty]
    private int _currentSessionNotificationsFiredCustom;

    public string LoginButtonText => _localizationService["account.login.button"];
    public string RegisterButtonText => _localizationService["account.register.button"];
    public string LogoutButtonText => _localizationService["account.logout.button"];
    public string EmailWatermark => _localizationService["account.email.watermark"];
    public string PasswordWatermark => _localizationService["account.password.watermark"];
    public string AccountHeader => _localizationService["account.header"];
    public string NotLoggedInText => _localizationService["account.not_logged_in"];
    
    // Свойства для статистики
    public string StatsHeader => _localizationService["account.stats.header"];
    public string ActiveTimeText => _localizationService["account.stats.active_time"];
    public string AfkTimeText => _localizationService["account.stats.afk_time"];
    public string NotifsFiredText => _localizationService["account.stats.notifs_fired"];

    public AccountViewModel(ApiService apiService, ConfigService configService, NotificationService notificationService, Action onAuthStateChanged)
    {
        _apiService = apiService;
        _configService = configService;
        _notificationService = notificationService;
        _onAuthStateChanged = onAuthStateChanged;
        
        _localizationService.LanguageChanged += (_) => UpdateLocalizedTexts();

        // Подписка на события статистики
        _notificationService.TotalTimeChanged += UpdateActiveTime;
        _notificationService.AfkTimeChanged += UpdateAfkTime;
        _notificationService.StatsChanged += UpdateNotificationsStats;

        // Проверяем сохраненный токен при запуске
        var config = _configService.LoadConfig();
        if (!string.IsNullOrEmpty(config.JwtToken))
        {
            _apiService.SetToken(config.JwtToken);
            CurrentUserEmail = config.UserEmail ?? "User";
            IsAuthenticated = true;
        }
        
        // Инициализация статистики
        UpdateActiveTime(_notificationService.TotalWorkTime);
        UpdateAfkTime(_notificationService.TotalAfkTime);
        UpdateNotificationsStats();
    }

    private void UpdateActiveTime(TimeSpan time)
    {
        CurrentSessionActiveTime = $"{time.Hours} ч. {time.Minutes} мин.";
    }

    private void UpdateAfkTime(TimeSpan time)
    {
        CurrentSessionAfkTime = $"{time.Hours} ч. {time.Minutes} мин.";
    }

    private void UpdateNotificationsStats()
    {
        CurrentSessionNotificationsFiredTotal = _notificationService.NotificationsFiredTotal;
        CurrentSessionNotificationsFiredCustom = _notificationService.NotificationsFiredCustom;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(EmailInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            ErrorMessage = "Email and Password are required.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        var (success, email, token, error) = await _apiService.LoginAsync(EmailInput, PasswordInput);
        
        IsLoading = false;

        if (success)
        {
            HandleSuccessfulAuth(email, token);
        }
        else
        {
            ErrorMessage = error ?? "Login failed.";
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(EmailInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            ErrorMessage = "Email and Password are required.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        var (success, email, token, error) = await _apiService.RegisterAsync(EmailInput, PasswordInput);
        
        IsLoading = false;

        if (success)
        {
            HandleSuccessfulAuth(email, token);
        }
        else
        {
            ErrorMessage = error ?? "Registration failed.";
        }
    }

    [RelayCommand]
    public void Logout()
    {
        _apiService.SetToken(null);
        IsAuthenticated = false;
        CurrentUserEmail = string.Empty;
        EmailInput = string.Empty;
        PasswordInput = string.Empty;
        ErrorMessage = string.Empty;

        var config = _configService.LoadConfig();
        config.JwtToken = null;
        config.UserEmail = null;
        _configService.SaveConfig(config);
        
        _onAuthStateChanged?.Invoke();
    }

    private void HandleSuccessfulAuth(string? email, string? token)
    {
        IsAuthenticated = true;
        CurrentUserEmail = email ?? "User";
        EmailInput = string.Empty;
        PasswordInput = string.Empty;
        ErrorMessage = string.Empty;

        var config = _configService.LoadConfig();
        config.JwtToken = token;
        config.UserEmail = email;
        _configService.SaveConfig(config);

        _onAuthStateChanged?.Invoke();
    }

    // Методы для синхронизации конфига и напоминаний с сервера
    public async Task<GlobalConfig?> FetchConfigFromServerAsync()
    {
        if (!IsAuthenticated) return null;
        return await _apiService.GetUserConfigAsync();
    }

    public async Task<List<ReminderConfig>?> FetchRemindersFromServerAsync()
    {
        if (!IsAuthenticated) return null;
        return await _apiService.GetUserRemindersAsync();
    }

    public async Task<bool> SyncConfigAsync(GlobalConfig config)
    {
        if (!IsAuthenticated) return false;
        return await _apiService.SyncConfigAsync(config);
    }

    public async Task<bool> SyncRemindersAsync(List<ReminderConfig> reminders)
    {
        if (!IsAuthenticated) return false;
        return await _apiService.SyncRemindersAsync(reminders);
    }

    public async Task<bool> SendDailyStatsAsync(string dateStr, long activeSeconds, long afkSeconds, int notifsTotal, int notifsCustom)
    {
        if (!IsAuthenticated) return false;
        return await _apiService.SendDailyStatsAsync(dateStr, activeSeconds, afkSeconds, notifsTotal, notifsCustom);
    }

    private void UpdateLocalizedTexts()
    {
        OnPropertyChanged(nameof(LoginButtonText));
        OnPropertyChanged(nameof(RegisterButtonText));
        OnPropertyChanged(nameof(LogoutButtonText));
        OnPropertyChanged(nameof(EmailWatermark));
        OnPropertyChanged(nameof(PasswordWatermark));
        OnPropertyChanged(nameof(AccountHeader));
        OnPropertyChanged(nameof(NotLoggedInText));
        OnPropertyChanged(nameof(StatsHeader));
        OnPropertyChanged(nameof(ActiveTimeText));
        OnPropertyChanged(nameof(AfkTimeText));
        OnPropertyChanged(nameof(NotifsFiredText));
    }
}
