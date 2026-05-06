using System;
using DeskQuit.Services.Localization;

namespace DeskQuit.Services.Notification;

public class NotificationTask(
    string id,
    string title,
    string text,
    TimeSpan interval,
    NotificationStyle style = NotificationStyle.Default,
    string? titleKey = null,
    string? textKey = null,
    bool isCustom = false)
{
    public string Id { get; } = id;

    public string Title { get; set; } = title; // Заголовок (что сделать)
    
    public string Text { get; set; } = text;
    
    public TimeSpan Interval { get; set; } = interval; // Как часто повторять
    
    public TimeSpan TimeLeft { get; set; } = interval; // Сколько осталось до следующего срабатывания
    
    public NotificationStyle Style { get; set; } = style;
    
    public string? TitleKey { get; set; } = titleKey;
    
    public string? TextKey { get; set; } = textKey;

    public bool IsCustom { get; } = isCustom;

    public bool IsHaveElapsedAction { get; private set; } = false;
    
    private Action<NotificationTask>? _elapsed;
    
    public event Action<NotificationTask>? Elapsed
    {
        add
        {
            IsHaveElapsedAction = true;
            _elapsed = value;
        }
        remove => _elapsed -= value;
    }

    public void Update(TimeSpan deltaTime)
    {
        TimeLeft -= deltaTime;
        
        if (TimeLeft > TimeSpan.Zero) 
            return;
        
        _elapsed?.Invoke(this);
        TimeLeft = Interval; // Перезапуск цикла
    }

    public string ResolveTitle(LocalizationService localizationService)
    {
        return string.IsNullOrWhiteSpace(TitleKey) ? Title : localizationService[TitleKey];
    }

    public string ResolveText(LocalizationService localizationService)
    {
        return string.IsNullOrWhiteSpace(TextKey) ? Text : localizationService[TextKey];
    }
}
