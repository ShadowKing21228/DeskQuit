using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DeskQuit.Models;
using DeskQuit.Services.Localization;
using DeskQuit.Services.Notification;

namespace DeskQuit.ViewModels;

public partial class ReminderSettingViewModel : ViewModelBase
{
    private readonly LocalizationService _localizationService = LocalizationService.Instance;

    public string Id { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private int _intervalInMinutes;

    [ObservableProperty]
    private bool _isIntervalDefault = true;

    // Внутреннее свойство стиля для связи с моделями
    public NotificationStyle StyleValue { get; private set; }

    // Свойство для ComboBox, содержащее локализованное имя и само значение стиля
    [ObservableProperty]
    private NotificationStyleOption _selectedStyleOption;

    public int DefaultIntervalInMinutes { get; }
    public string NotificationTitleKey { get; }
    public string NotificationBodyKey { get; }

    public List<NotificationStyleOption> AvailableStyles { get; private set; } = new();

    public ReminderSettingViewModel(
        string id,
        string title,
        string description,
        string source,
        bool isEnabled,
        int intervalInMinutes,
        string notificationTitleKey,
        string notificationBodyKey,
        NotificationStyle style)
    {
        Id = id;
        Title = title;
        Description = description;
        Source = source;
        IsEnabled = isEnabled;
        IntervalInMinutes = intervalInMinutes;
        DefaultIntervalInMinutes = intervalInMinutes;
        NotificationTitleKey = notificationTitleKey;
        NotificationBodyKey = notificationBodyKey;
        
        StyleValue = style;

        UpdateLocalizedStyles();
        
        _selectedStyleOption = AvailableStyles.FirstOrDefault(s => s.Style == style) ?? AvailableStyles[0];

        this.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IntervalInMinutes))
            {
                IsIntervalDefault = (IntervalInMinutes == DefaultIntervalInMinutes);
            }
            else if (e.PropertyName == nameof(SelectedStyleOption) && SelectedStyleOption != null)
            {
                StyleValue = SelectedStyleOption.Style;
            }
        };
    }

    public void UpdateLocalizedStyles()
    {
        var currentStyle = SelectedStyleOption?.Style ?? StyleValue;
        
        AvailableStyles = new List<NotificationStyleOption>
        {
            new() { Style = NotificationStyle.Default, DisplayName = _localizationService["style.default"] },
            new() { Style = NotificationStyle.SoftPersistentTelegram, DisplayName = _localizationService["style.soft"] },
            new() { Style = NotificationStyle.AggressiveBlocking, DisplayName = _localizationService["style.aggressive"] }
        };
        
        OnPropertyChanged(nameof(AvailableStyles));
        SelectedStyleOption = AvailableStyles.FirstOrDefault(s => s.Style == currentStyle) ?? AvailableStyles[0];
    }
}
