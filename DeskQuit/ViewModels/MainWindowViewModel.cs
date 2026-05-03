using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    private FloatingTimerWindow? _floatingTimerWindow;

    public ObservableCollection<ReminderSettingViewModel> Reminders { get; } = new();

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

    // Properties for localized UI strings
    public string RemindersTabHeader => _localizationService["main.tab.reminders"];
    public string SettingsTabHeader => _localizationService["main.tab.settings"];
    public string InfoTabHeader => _localizationService["main.tab.info"];
    public string ToggleTimerButtonText => _localizationService["main.reminders.toggle_timer.button"];
    public string AfkThresholdLabel => _localizationService["main.settings.afk_threshold"];
    
    // Info tab content
    public string InfoContent => _localizationService["main.info.content"];

    public MainWindowViewModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
        _configService = new ConfigService();
        _localizationService.LanguageChanged += OnLanguageChanged;
        
        InitializeData();
    }

    private void InitializeData()
    {
        Reminders.Clear();
        _notificationService.ClearTasks();
        
        // Load saved config
        var globalConfig = _configService.LoadConfig();
        
        _afkThresholdMinutes = globalConfig.AfkThresholdMinutes > 0 ? globalConfig.AfkThresholdMinutes : 1;
        _notificationService.SetAfkThreshold(TimeSpan.FromMinutes(_afkThresholdMinutes));
        OnPropertyChanged(nameof(AfkThresholdMinutes));

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

        foreach (var reminder in Reminders)
        {
            reminder.PropertyChanged += (_, e) => OnReminderChanged(reminder, e.PropertyName);
            if (reminder.IsEnabled)
            {
                _notificationService.AddTask(CreateTaskFromVm(reminder));
            }
        }
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
        var existingTask = _notificationService.FindTask(reminder.NotificationTitleKey);

        if (propertyName == nameof(ReminderSettingViewModel.IsEnabled))
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
        }
        else if (propertyName == nameof(ReminderSettingViewModel.IntervalInMinutes))
        {
            if (existingTask != null)
            {
                existingTask.Interval = TimeSpan.FromMinutes(reminder.IntervalInMinutes);
                // Reset timer to new interval to apply immediately
                existingTask.TimeLeft = existingTask.Interval;
            }
            SaveConfiguration();
        }
        else if (propertyName == nameof(ReminderSettingViewModel.SelectedStyleOption))
        {
            if (existingTask != null)
            {
                existingTask.Style = reminder.StyleValue;
            }
            SaveConfiguration();
        }
    }

    private void SaveConfiguration()
    {
        var config = new GlobalConfig
        {
            AfkThresholdMinutes = this.AfkThresholdMinutes,
            Reminders = Reminders.Select(r => new ReminderConfig
            {
                Id = r.Id,
                IsEnabled = r.IsEnabled,
                IntervalInMinutes = r.IntervalInMinutes,
                NotificationStyle = r.StyleValue
            }).ToList()
        };
        
        _configService.SaveConfig(config);
    }

    private NotificationTask CreateTaskFromVm(ReminderSettingViewModel vm)
    {
        return new NotificationTask(
            title: "", // Title and Body are now resolved via keys
            text: "",
            interval: TimeSpan.FromMinutes(vm.IntervalInMinutes),
            style: vm.StyleValue,
            titleKey: vm.NotificationTitleKey,
            textKey: vm.NotificationBodyKey
        );
    }

    private void OnLanguageChanged(AppLanguage _)
    {
        UpdateLocalizedTexts();
        
        OnPropertyChanged(nameof(RemindersTabHeader));
        OnPropertyChanged(nameof(SettingsTabHeader));
        OnPropertyChanged(nameof(InfoTabHeader));
        OnPropertyChanged(nameof(ToggleTimerButtonText));
        OnPropertyChanged(nameof(AfkThresholdLabel));
        OnPropertyChanged(nameof(InfoContent));
    }

    private void UpdateLocalizedTexts()
    {
        foreach (var r in Reminders)
        {
            if (r.Id == "eyes")
            {
                r.Title = _localizationService["main.reminders.eyes.title"];
                r.Description = _localizationService["main.reminders.eyes.description"];
                r.Source = _localizationService["main.reminders.eyes.source"];
            }
            else if (r.Id == "neck")
            {
                r.Title = _localizationService["main.reminders.neck.title"];
                r.Description = _localizationService["main.reminders.neck.description"];
                r.Source = _localizationService["main.reminders.neck.source"];
            }
            else if (r.Id == "back")
            {
                r.Title = _localizationService["main.reminders.back.title"];
                r.Description = _localizationService["main.reminders.back.description"];
                r.Source = _localizationService["main.reminders.back.source"];
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
                DataContext = new FloatingTimerViewModel(_notificationService)
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
