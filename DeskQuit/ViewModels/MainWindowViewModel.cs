using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Data.Core;
using CommunityToolkit.Mvvm.Input;
using DeskQuit.Models;
using DeskQuit.Services;
using DeskQuit.Services.Localization;
using DeskQuit.Services.Notification;
using DeskQuit.Views;

namespace DeskQuit.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NotificationService _notificationService;
    private readonly ConfigService _configService;
    private readonly ApiService _apiService;
    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    private FloatingTimerWindow? _floatingTimerWindow;

    public AccountViewModel AccountViewModel { get; }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                OnPropertyChanged(nameof(FilteredReminders));
            }
        }
    }

    public ObservableCollection<ReminderSettingViewModel> Reminders { get; } = new();

    public System.Collections.Generic.IEnumerable<ReminderSettingViewModel> FilteredReminders => 
        string.IsNullOrWhiteSpace(SearchQuery) 
            ? Reminders 
            : Reminders.Where(r => r.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                 r.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

    // Global Settings
    private int _afkThresholdMinutes;
    public int AfkThresholdMinutes
    {
        get => _afkThresholdMinutes;
        set
        {
            if (SetProperty(ref _afkThresholdMinutes, value))
            {
                _notificationService.SetAfkThreshold(TimeSpan.FromMinutes(value));
                SaveConfiguration();
            }
        }
    }
    
    private bool _runOnStartup;
    public bool RunOnStartup
    {
        get => _runOnStartup;
        set
        {
            if (SetProperty(ref _runOnStartup, value))
            {
                StartupService.SetRunOnStartup(value);
                SaveConfiguration();
            }
        }
    }

    private string _serverUrl = string.Empty;
    public string ServerUrl
    {
        get => _serverUrl;
        set
        {
            if (SetProperty(ref _serverUrl, value))
            {
                _apiService.UpdateBaseUrl(value);
                SaveConfiguration();
            }
        }
    }

    private double _timerWidth;
    public double TimerWidth
    {
        get => _timerWidth;
        set
        {
            if (SetProperty(ref _timerWidth, value))
            {
                if (_floatingTimerWindow != null)
                {
                    _floatingTimerWindow.Width = value;
                }
                SaveConfiguration();
            }
        }
    }

    private double _timerHeight;
    public double TimerHeight
    {
        get => _timerHeight;
        set
        {
            if (SetProperty(ref _timerHeight, value))
            {
                if (_floatingTimerWindow != null)
                {
                    _floatingTimerWindow.Height = value;
                }
                SaveConfiguration();
            }
        }
    }

    // Properties for localized UI strings
    public string RemindersTabHeader => _localizationService["main.tab.reminders"];
    public string SettingsTabHeader => _localizationService["main.tab.settings"];
    public string InfoTabHeader => _localizationService["main.tab.info"];
    public string TimerTabHeader => _localizationService["main.tab.timer"];
    public string AccountTabHeader => _localizationService["main.tab.account"];
    public string ToggleTimerButtonText => _localizationService["main.reminders.toggle_timer.button"];
    public string AfkThresholdLabel => _localizationService["main.settings.afk_threshold"];
    public string RunOnStartupLabel => _localizationService["main.settings.run_on_startup"];
    public string ServerUrlLabel => _localizationService["main.settings.server_url"];
    public string AddCustomReminderButtonText => _localizationService["main.reminders.add_custom.button"];
    public string SearchWatermarkText => _localizationService["main.reminders.search.watermark"];
    public string TimerWidthLabel => _localizationService["main.timer.width"];
    public string TimerHeightLabel => _localizationService["main.timer.height"];
    
    // Info tab content
    public string InfoContent => _localizationService["main.info.content"];

    public MainWindowViewModel(NotificationService notificationService, ApiService apiService)
    {
        _notificationService = notificationService;
        _apiService = apiService;
        _configService = new ConfigService();
        _localizationService.LanguageChanged += OnLanguageChanged;
        
        AccountViewModel = new AccountViewModel(apiService, _configService, _notificationService, OnAuthStateChanged);

        Reminders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FilteredReminders));

        InitializeData();
    }

    private async void OnAuthStateChanged()
    {
        // When auth state changes, we should sync config
        await SyncConfigWithServer();
    }

    private async Task SyncConfigWithServer()
    {
        if (AccountViewModel.IsAuthenticated)
        {
            // Fetch config from server
            var serverConfig = await AccountViewModel.FetchConfigFromServerAsync();
            if (serverConfig != null)
            {
                _afkThresholdMinutes = serverConfig.AfkThresholdMinutes;
                _timerWidth = serverConfig.TimerWidth;
                _timerHeight = serverConfig.TimerHeight;
                _runOnStartup = serverConfig.RunOnStartup;
                
                OnPropertyChanged(nameof(AfkThresholdMinutes));
                OnPropertyChanged(nameof(TimerWidth));
                OnPropertyChanged(nameof(TimerHeight));
                OnPropertyChanged(nameof(RunOnStartup));
                StartupService.SetRunOnStartup(serverConfig.RunOnStartup);

                // Fetch reminders from server
                var serverReminders = await AccountViewModel.FetchRemindersFromServerAsync();
                if (serverReminders != null)
                {
                    Reminders.Clear();
                    _notificationService.ClearTasks();

                    // Re-add default reminders
                    var eyesReminder = CreateReminderVM(
                        id: "eyes",
                        titleKey: "main.reminders.eyes.title",
                        descKey: "main.reminders.eyes.description",
                        sourceKey: "main.reminders.eyes.source",
                        defaultInterval: 20,
                        notifTitleKey: "notification.eyes.title",
                        notifBodyKey: "notification.eyes.body",
                        defaultStyle: NotificationStyle.SoftPersistentTelegram,
                        savedConfigs: serverReminders
                    );
                    var neckReminder = CreateReminderVM(
                        id: "neck",
                        titleKey: "main.reminders.neck.title",
                        descKey: "main.reminders.neck.description",
                        sourceKey: "main.reminders.neck.source",
                        defaultInterval: 45,
                        notifTitleKey: "notification.neck.title",
                        notifBodyKey: "notification.neck.body",
                        defaultStyle: NotificationStyle.SoftPersistentTelegram,
                        savedConfigs: serverReminders
                    );
                    var backReminder = CreateReminderVM(
                        id: "back",
                        titleKey: "main.reminders.back.title",
                        descKey: "main.reminders.back.description",
                        sourceKey: "main.reminders.back.source",
                        defaultInterval: 60,
                        notifTitleKey: "notification.back.title",
                        notifBodyKey: "notification.back.body",
                        defaultStyle: NotificationStyle.AggressiveBlocking,
                        savedConfigs: serverReminders
                    );
                    
                    Reminders.Add(eyesReminder);
                    Reminders.Add(neckReminder);
                    Reminders.Add(backReminder);

                    // Add custom reminders from server
                    foreach (var savedConfig in serverReminders.Where(c => c.IsCustom))
                    {
                        var customVm = new ReminderSettingViewModel(
                            id: savedConfig.Id,
                            title: savedConfig.CustomTitle ?? "",
                            description: savedConfig.CustomDescription ?? "",
                            source: "",
                            isEnabled: savedConfig.IsEnabled,
                            intervalInMinutes: savedConfig.IntervalInMinutes,
                            notificationTitleKey: "",
                            notificationBodyKey: "",
                            style: savedConfig.NotificationStyle,
                            isCustom: true,
                            titleWatermark: _localizationService["main.reminders.custom.default_title"],
                            descriptionWatermark: _localizationService["main.reminders.custom.default_description"]
                        );
                        Reminders.Add(customVm);
                    }

                    foreach (var reminder in Reminders)
                    {
                        reminder.PropertyChanged += OnReminderPropertyChanged;
                        reminder.DeleteRequested += OnReminderDeleted;
                        if (reminder.IsEnabled)
                        {
                            _notificationService.AddTask(CreateTaskFromVm(reminder));
                        }
                    }
                }
            }
            else
            {
                // If fetching from server fails or doesn't exist, send current local config to server
                await AccountViewModel.SyncConfigAsync(_configService.LoadConfig());
                await AccountViewModel.SyncRemindersAsync(Reminders.Select(r => new ReminderConfig
                {
                    Id = r.Id,
                    IsEnabled = r.IsEnabled,
                    IntervalInMinutes = r.IntervalInMinutes,
                    NotificationStyle = r.StyleValue,
                    IsCustom = r.IsCustom,
                    CustomTitle = r.IsCustom ? r.Title : null,
                    CustomDescription = r.IsCustom ? r.Description : null
                }).ToList());
            }
        }
    }

    private void InitializeData()
    {
        Reminders.Clear();
        _notificationService.ClearTasks();
        
        // Load saved config
        var globalConfig = _configService.LoadConfig();
        
        _serverUrl = string.IsNullOrWhiteSpace(globalConfig.ServerUrl) ? "http://localhost:8080/api/" : globalConfig.ServerUrl;
        _apiService.UpdateBaseUrl(_serverUrl);
        OnPropertyChanged(nameof(ServerUrl));

        _afkThresholdMinutes = globalConfig.AfkThresholdMinutes > 0 ? globalConfig.AfkThresholdMinutes : 1;
        _notificationService.SetAfkThreshold(TimeSpan.FromMinutes(_afkThresholdMinutes));
        OnPropertyChanged(nameof(AfkThresholdMinutes));

        _runOnStartup = StartupService.GetRunOnStartup();
        // Sync config with OS settings just in case
        if (_runOnStartup != globalConfig.RunOnStartup)
        {
            globalConfig.RunOnStartup = _runOnStartup;
            _configService.SaveConfig(globalConfig);
        }
        OnPropertyChanged(nameof(RunOnStartup));

        _timerWidth = globalConfig.TimerWidth > 0 ? globalConfig.TimerWidth : 180;
        OnPropertyChanged(nameof(TimerWidth));
        _timerHeight = globalConfig.TimerHeight > 0 ? globalConfig.TimerHeight : 60;
        OnPropertyChanged(nameof(TimerHeight));

        // Загружаем статистику после установления BaseAddress
        _ = AccountViewModel.RefreshTotalStatsAsync();

        var savedConfigs = globalConfig.Reminders;

        var eyesReminder = CreateReminderVM(
            id: "eyes",
            titleKey: "main.reminders.eyes.title",
            descKey: "main.reminders.eyes.description",
            sourceKey: "main.reminders.eyes.source",
            defaultInterval: 20,
            notifTitleKey: "notification.eyes.title",
            notifBodyKey: "notification.eyes.body",
            defaultStyle: NotificationStyle.SoftPersistentTelegram,
            savedConfigs: savedConfigs
        );

        var neckReminder = CreateReminderVM(
            id: "neck",
            titleKey: "main.reminders.neck.title",
            descKey: "main.reminders.neck.description",
            sourceKey: "main.reminders.neck.source",
            defaultInterval: 45,
            notifTitleKey: "notification.neck.title",
            notifBodyKey: "notification.neck.body",
            defaultStyle: NotificationStyle.SoftPersistentTelegram,
            savedConfigs: savedConfigs
        );

        var backReminder = CreateReminderVM(
            id: "back",
            titleKey: "main.reminders.back.title",
            descKey: "main.reminders.back.description",
            sourceKey: "main.reminders.back.source",
            defaultInterval: 60,
            notifTitleKey: "notification.back.title",
            notifBodyKey: "notification.back.body",
            defaultStyle: NotificationStyle.AggressiveBlocking,
            savedConfigs: savedConfigs
        );
        
        Reminders.Add(eyesReminder);
        Reminders.Add(neckReminder);
        Reminders.Add(backReminder);

        foreach (var savedConfig in savedConfigs.Where(c => c.IsCustom))
        {
            var customVm = new ReminderSettingViewModel(
                id: savedConfig.Id,
                title: savedConfig.CustomTitle ?? "",
                description: savedConfig.CustomDescription ?? "",
                source: "",
                isEnabled: savedConfig.IsEnabled,
                intervalInMinutes: savedConfig.IntervalInMinutes,
                notificationTitleKey: "",
                notificationBodyKey: "",
                style: savedConfig.NotificationStyle,
                isCustom: true,
                titleWatermark: _localizationService["main.reminders.custom.default_title"],
                descriptionWatermark: _localizationService["main.reminders.custom.default_description"]
            );
            Reminders.Add(customVm);
        }

        foreach (var reminder in Reminders)
        {
            reminder.PropertyChanged += OnReminderPropertyChanged;
            reminder.DeleteRequested += OnReminderDeleted;
            if (reminder.IsEnabled)
            {
                _notificationService.AddTask(CreateTaskFromVm(reminder));
            }
        }
        
        // Ensure initial sync attempt if logged in
        if (AccountViewModel.IsAuthenticated)
        {
            _ = SyncConfigWithServer();
        }
    }

    private void OnReminderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ReminderSettingViewModel reminder)
        {
            OnReminderChanged(reminder, e.PropertyName);
            if (e.PropertyName == nameof(ReminderSettingViewModel.Title) || e.PropertyName == nameof(ReminderSettingViewModel.Description))
            {
                OnPropertyChanged(nameof(FilteredReminders));
            }
        }
    }

    private void OnReminderDeleted(ReminderSettingViewModel reminder)
    {
        Reminders.Remove(reminder);
        reminder.PropertyChanged -= OnReminderPropertyChanged;
        reminder.DeleteRequested -= OnReminderDeleted;

        var existingTask = _notificationService.FindTask(reminder.Id);
        if (existingTask != null)
        {
            _notificationService.RemoveTask(existingTask);
        }

        SaveConfiguration();
    }

    [RelayCommand]
    private void AddCustomReminder()
    {
        var customVm = new ReminderSettingViewModel(
            id: Guid.NewGuid().ToString(),
            title: "",
            description: "",
            source: "",
            isEnabled: true,
            intervalInMinutes: 30,
            notificationTitleKey: "",
            notificationBodyKey: "",
            style: NotificationStyle.SoftPersistentTelegram,
            isCustom: true,
            isEditing: true,
            titleWatermark: _localizationService["main.reminders.custom.default_title"],
            descriptionWatermark: _localizationService["main.reminders.custom.default_description"]
        );

        customVm.PropertyChanged += OnReminderPropertyChanged;
        customVm.DeleteRequested += OnReminderDeleted;
        Reminders.Add(customVm);
        _notificationService.AddTask(CreateTaskFromVm(customVm));
        SaveConfiguration();
    }

    private ReminderSettingViewModel CreateReminderVM(
        string id, string titleKey, string descKey, string sourceKey, 
        int defaultInterval, string notifTitleKey, string notifBodyKey, 
        NotificationStyle defaultStyle, System.Collections.Generic.List<ReminderConfig> savedConfigs)
    {
        var savedConfig = savedConfigs.FirstOrDefault(c => c.Id == id);
        
        return new ReminderSettingViewModel(
            id: id,
            title: _localizationService[titleKey],
            description: _localizationService[descKey],
            source: _localizationService[sourceKey],
            isEnabled: savedConfig?.IsEnabled ?? true,
            intervalInMinutes: savedConfig?.IntervalInMinutes ?? defaultInterval,
            notificationTitleKey: notifTitleKey,
            notificationBodyKey: notifBodyKey,
            style: savedConfig != null ? savedConfig.NotificationStyle : defaultStyle
        );
    }

    private void OnReminderChanged(ReminderSettingViewModel reminder, string? propertyName)
    {
        var existingTask = _notificationService.FindTask(reminder.Id);

        switch (propertyName)
        {
            case nameof(ReminderSettingViewModel.IsEnabled):
            {
                if (reminder.IsEnabled)
                {
                    if (existingTask == null)
                        _notificationService.AddTask(CreateTaskFromVm(reminder));
                }
                else
                {
                    if (existingTask != null)
                        _notificationService.RemoveTask(existingTask);
                }
                SaveConfiguration();
                break;
            }
            case nameof(ReminderSettingViewModel.IntervalInMinutes):
            {
                if (existingTask != null)
                {
                    existingTask.Interval = TimeSpan.FromMinutes(reminder.IntervalInMinutes);
                    // Reset timer to new interval to apply immediately
                    existingTask.TimeLeft = existingTask.Interval;
                }
                SaveConfiguration();
                break;
            }
            case nameof(ReminderSettingViewModel.SelectedStyleOption):
            {
                if (existingTask != null)
                {
                    existingTask.Style = reminder.StyleValue;
                }
                SaveConfiguration();
                break;
            }
            case nameof(ReminderSettingViewModel.Title):
            case nameof(ReminderSettingViewModel.Description):
            {
                if (existingTask != null)
                {
                    existingTask.Title = reminder.Title;
                    existingTask.Text = reminder.Description;
                }
                SaveConfiguration();
                break;
            }
        }
    }

    private void SaveConfiguration()
    {
        var config = new GlobalConfig
        {
            AfkThresholdMinutes = this.AfkThresholdMinutes,
            TimerWidth = this.TimerWidth,
            TimerHeight = this.TimerHeight,
            RunOnStartup = this.RunOnStartup,
            ServerUrl = this.ServerUrl,
            JwtToken = _configService.LoadConfig().JwtToken,
            UserEmail = _configService.LoadConfig().UserEmail,
            Reminders = Reminders.Select(r => new ReminderConfig
            {
                Id = r.Id,
                IsEnabled = r.IsEnabled,
                IntervalInMinutes = r.IntervalInMinutes,
                NotificationStyle = r.StyleValue,
                IsCustom = r.IsCustom,
                CustomTitle = r.IsCustom ? r.Title : null,
                CustomDescription = r.IsCustom ? r.Description : null
            }).ToList()
        };
        
        _configService.SaveConfig(config);
        
        if (AccountViewModel.IsAuthenticated)
        {
            _ = AccountViewModel.SyncConfigAsync(config);
            _ = AccountViewModel.SyncRemindersAsync(config.Reminders);
        }
    }

    private NotificationTask CreateTaskFromVm(ReminderSettingViewModel vm)
    {
        return new NotificationTask(
            id: vm.Id,
            title: vm.IsCustom ? vm.Title : "",
            text: vm.IsCustom ? vm.Description : "",
            interval: TimeSpan.FromMinutes(vm.IntervalInMinutes),
            style: vm.StyleValue,
            titleKey: vm.IsCustom ? null : vm.NotificationTitleKey,
            textKey: vm.IsCustom ? null : vm.NotificationBodyKey,
            isCustom: vm.IsCustom
        );
    }

    private void OnLanguageChanged(AppLanguage _)
    {
        UpdateLocalizedTexts();
        
        OnPropertyChanged(nameof(RemindersTabHeader));
        OnPropertyChanged(nameof(SettingsTabHeader));
        OnPropertyChanged(nameof(InfoTabHeader));
        OnPropertyChanged(nameof(TimerTabHeader));
        OnPropertyChanged(nameof(AccountTabHeader));
        OnPropertyChanged(nameof(ToggleTimerButtonText));
        OnPropertyChanged(nameof(AfkThresholdLabel));
        OnPropertyChanged(nameof(RunOnStartupLabel));
        OnPropertyChanged(nameof(ServerUrlLabel));
        OnPropertyChanged(nameof(InfoContent));
        OnPropertyChanged(nameof(AddCustomReminderButtonText));
        OnPropertyChanged(nameof(SearchWatermarkText));
        OnPropertyChanged(nameof(TimerWidthLabel));
        OnPropertyChanged(nameof(TimerHeightLabel));
    }

    private void UpdateLocalizedTexts()
    {
        foreach (var r in Reminders)
        {
            switch (r.Id)
            {
                case "eyes":
                    r.Title = _localizationService["main.reminders.eyes.title"];
                    r.Description = _localizationService["main.reminders.eyes.description"];
                    r.Source = _localizationService["main.reminders.eyes.source"];
                    break;
                case "neck":
                    r.Title = _localizationService["main.reminders.neck.title"];
                    r.Description = _localizationService["main.reminders.neck.description"];
                    r.Source = _localizationService["main.reminders.neck.source"];
                    break;
                case "back":
                    r.Title = _localizationService["main.reminders.back.title"];
                    r.Description = _localizationService["main.reminders.back.description"];
                    r.Source = _localizationService["main.reminders.back.source"];
                    break;
            }

            r.UpdateLocalizedStyles();
        }
    }

    [RelayCommand]
    private void ToggleFloatingTimer()
    {
        if (_floatingTimerWindow == null)
        {
            _floatingTimerWindow = new FloatingTimerWindow
            {
                DataContext = new FloatingTimerViewModel(_notificationService),
                Width = TimerWidth,
                Height = TimerHeight
            };
            _floatingTimerWindow.Closed += (_, _) => _floatingTimerWindow = null;
            _floatingTimerWindow.Show();
        }
        else
        {
            _floatingTimerWindow.Close();
            _floatingTimerWindow = null;
        }
    }
}
