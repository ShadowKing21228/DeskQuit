using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeskQuit.Services.Localization;

public enum AppLanguage
{
    Russian = 0,
    English = 1
}

public sealed class LocalizationService
{
    public static LocalizationService Instance { get; } = new();

    private readonly Dictionary<string, (string Ru, string En)> _translations = new()
    {
        ["app.tray.tooltip"] = ("DeskQuit — Следим за здоровьем", "DeskQuit — Health tracker"),
        ["app.tray.open"] = ("Открыть DeskQuit", "Open DeskQuit"),
        ["app.tray.exit"] = ("Выход", "Exit"),
        ["app.tray.language"] = ("Язык", "Language"),
        ["app.tray.language.ru"] = ("Русский", "Russian"),
        ["app.tray.language.en"] = ("Английский", "English"),
        ["main.greeting"] = ("Добро пожаловать в DeskQuit!", "Welcome to DeskQuit!"),
        ["notification.soft.title"] = ("Мягкое напоминание", "Gentle reminder"),
        ["notification.soft.body"] = ("Пора встать, размяться и дать глазам отдохнуть.", "Time to stand up, stretch, and rest your eyes."),
        ["notification.aggressive.title"] = ("Перерыв обязателен", "Break is mandatory"),
        ["notification.aggressive.body"] = ("Остановись на минуту: сделай паузу и короткую разминку.", "Stop for a minute: take a pause and do a short stretch."),
        ["notification.soft.snooze"] = ("Напомнить через 5 мин", "Remind me in 5 min"),
        ["notification.soft.done"] = ("Сделано", "Done"),
        ["notification.aggressive.start.in"] = ("Начать перерыв через {0}", "Start break in {0}"),
        ["notification.aggressive.start.now"] = ("Начинаю перерыв", "Starting break")
    };

    private LocalizationService()
    {
    }

    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Russian;

    public event Action<AppLanguage>? LanguageChanged;

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (!_translations.TryGetValue(key, out var value))
            return key;

        return CurrentLanguage == AppLanguage.Russian ? value.Ru : value.En;
    }

    public string Format(string key, params object[] args)
    {
        var format = Get(key);
        return string.Format(CultureInfo.CurrentUICulture, format, args);
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
            return;

        CurrentLanguage = language;

        var cultureName = language == AppLanguage.Russian ? "ru-RU" : "en-US";
        var culture = CultureInfo.GetCultureInfo(cultureName);

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;

        LanguageChanged?.Invoke(language);
    }

    public AppLanguage DetectSystemLanguage()
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Russian
            : AppLanguage.English;
    }
}
