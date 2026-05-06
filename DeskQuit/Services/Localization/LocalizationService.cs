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
        ["app.tray.tooltip"] = ("DeskQuit — Забота о вашем здоровье за ПК", "DeskQuit — Health tracker"),
        ["app.tray.open"] = ("Открыть DeskQuit", "Open DeskQuit"),
        ["app.tray.exit"] = ("Выход", "Exit"),
        ["app.tray.language"] = ("Язык", "Language"),
        ["app.tray.language.ru"] = ("Русский", "Russian"),
        ["app.tray.language.en"] = ("Английский", "English"),
        
        ["main.greeting"] = ("Добро пожаловать в DeskQuit!", "Welcome to DeskQuit!"),
        ["main.tab.reminders"] = ("Напоминания", "Reminders"),
        ["main.tab.settings"] = ("Настройки", "Settings"),
        ["main.tab.info"] = ("Справка", "Info"),
        ["main.tab.timer"] = ("Таймер", "Timer"),
        ["main.tab.account"] = ("Аккаунт", "Account"),
        ["main.settings.afk_threshold"] = ("Время простоя до включения паузы (мин):", "AFK threshold before pausing timers (min):"),
        ["main.settings.run_on_startup"] = ("Запускать при старте системы", "Run on system startup"),
        ["main.settings.server_url"] = ("Адрес сервера синхронизации:", "Sync server URL:"),
        ["main.reminders.warning"] = ("Вы отклонились от рекомендованного интервала.", "You have deviated from the recommended interval."),
        ["main.reminders.toggle_timer.button"] = ("Показать/скрыть таймер", "Show/Hide Timer"),
        ["main.reminders.notification_style"] = ("Стиль уведомления:", "Notification Style:"),
        ["main.reminders.add_custom.button"] = ("+ Добавить свое напоминание", "+ Add custom reminder"),
        ["main.reminders.custom.default_title"] = ("Новое напоминание", "New reminder"),
        ["main.reminders.custom.default_description"] = ("Описание напоминания", "Reminder description"),
        ["main.reminders.search.watermark"] = ("Поиск напоминаний...", "Search reminders..."),
        
        ["main.timer.width"] = ("Ширина таймера:", "Timer Width:"),
        ["main.timer.height"] = ("Высота таймера:", "Timer Height:"),

        ["account.login.button"] = ("Войти", "Log in"),
        ["account.register.button"] = ("Зарегистрироваться", "Register"),
        ["account.logout.button"] = ("Выйти", "Log out"),
        ["account.email.watermark"] = ("Электронная почта", "Email"),
        ["account.password.watermark"] = ("Пароль", "Password"),
        ["account.header"] = ("Синхронизация и Статистика", "Sync and Statistics"),
        ["account.not_logged_in"] = ("Войдите в аккаунт, чтобы сохранять свои настройки и просматривать расширенную статистику в будущем.", "Log in to save your settings and view detailed statistics in the future."),
        ["account.stats.header"] = ("Ваша статистика (текущая сессия)", "Your statistics (current session)"),
        ["account.stats.active_time"] = ("Активное время:", "Active time:"),
        ["account.stats.afk_time"] = ("Время бездействия (AFK):", "AFK time:"),
        ["account.stats.notifs_fired"] = ("Сработало уведомлений:", "Notifications fired:"),
        ["account.stats.total_header"] = ("Статистика (в целом)", "Statistics (overall)"),
        ["account.stats.days_tracked"] = ("Дней отслеживания:", "Days tracked:"),
        ["account.stats.date_range"] = ("Период:", "Period:"),

        ["style.default"] = ("Системное уведомление", "System Notification"),
        ["style.soft"] = ("Всплывающее окно DeskQuit", "DeskQuit Popup Window"),
        ["style.aggressive"] = ("Полная блокировка экрана", "Full Screen Block"),

        ["main.info.content"] = ("DeskQuit создан на основе медицинских рекомендаций по гигиене труда.\n\n" +
            "1. Глаза (Снятие спазма аккомодации):\n" +
            "Мы используем правило «20-20-20» (Американская Академия Офтальмологии): каждые 20 минут отводите взгляд на объект в 6 метрах (20 футах) от вас на 20 секунд. Это позволяет цилиарной мышце глаза расслабиться после напряжения от близкого фокуса.\n\n" +
            "2. Шея (Профилактика «синдрома текстовой шеи»):\n" +
            "При долгой работе за ПК голова часто выдвигается вперед, многократно увеличивая нагрузку на шейный отдел (до 27 кг). Короткие разминки (наклоны, повороты) помогают разогнать молочную кислоту и восстановить кровоток в мозг (на основе рекомендаций OSHA).\n\n" +
            "3. Спина (Раскрытие грудного отдела):\n" +
            "Длительное сидение приводит к компрессии поясничных дисков и сутулости. Обязательно нужно вставать раз в час (согласно нормам СанПиН), вытягиваться вверх и сводить лопатки, чтобы снять гипертонус с грудных мышц и включить мышцы спины.", 
            "DeskQuit is built on medical guidelines for occupational health.\n\n" +
            "1. Eyes (Relieving accommodation spasm):\n" +
            "We use the '20-20-20' rule (American Academy of Ophthalmology): every 20 minutes, look at an object 20 feet away for 20 seconds. This allows the eye's ciliary muscle to relax after near-focus strain.\n\n" +
            "2. Neck (Preventing 'text neck' syndrome):\n" +
            "During long PC sessions, the head often protrudes forward, exponentially increasing the load on the cervical spine (up to 60 lbs). Short stretches help clear lactic acid and restore blood flow to the brain (based on OSHA recommendations).\n\n" +
            "3. Back (Thoracic extension):\n" +
            "Prolonged sitting leads to lumbar disc compression and slouching. It is mandatory to stand up once an hour, stretch upward, and squeeze your shoulder blades to relieve hypertonus in the chest muscles and engage the back muscles."),

        ["main.reminders.eyes.title"] = ("Отдых для глаз", "Eye Rest"),
        ["main.reminders.eyes.description"] = ("Снятие спазма аккомодации, который возникает при долгой фокусировке на близких объектах (монитор).", "Relieving accommodation spasm that occurs during long focus on close objects (monitor)."),
        ["main.reminders.eyes.source"] = ("Источник: Правило 20-20-20 (AAO) и гимнастика по Э. С. Аветисову (РФ).", "Source: 20-20-20 Rule (AAO) and Avetisov's gymnastics (RF)."),

        ["main.reminders.neck.title"] = ("Разминка для шеи", "Neck Stretch"),
        ["main.reminders.neck.description"] = ("Профилактика «синдрома текстовой шеи» и зажимов в шейном отделе позвоночника.", "Prevention of 'text neck syndrome' and clamps in the cervical spine."),
        ["main.reminders.neck.source"] = ("Источник: Рекомендации OSHA (США) и методики ЛФК (РФ).", "Source: OSHA recommendations (USA) and physical therapy methods (RF)."),

        ["main.reminders.back.title"] = ("Разминка для спины", "Back Stretch"),
        ["main.reminders.back.description"] = ("Раскрытие грудного отдела и снятие компрессионной нагрузки с поясницы.", "Opening the thoracic region and relieving compression load from the lower back."),
        ["main.reminders.back.source"] = ("Источник: Клинические рекомендации Mayo Clinic и нормы СанПиН (РФ).", "Source: Mayo Clinic clinical recommendations and SanPiN standards (RF)."),

        ["notification.soft.title"] = ("Пора отдохнуть!", "Time to rest!"),
        ["notification.soft.body"] = ("Сделайте небольшой перерыв.", "Take a short break."),
        ["notification.aggressive.title"] = ("Перерыв обязателен", "Break is mandatory"),
        ["notification.aggressive.body"] = ("Остановись на минуту: сделай паузу и короткую разминку.", "Stop for a minute: take a pause and do a short stretch."),
        ["notification.soft.snooze"] = ("Напомнить через 5 мин", "Remind me in 5 min"),
        ["notification.soft.done"] = ("Сделано", "Done"),
        ["notification.aggressive.start.in"] = ("Начать перерыв через {0}", "Start break in {0}"),
        ["notification.aggressive.start.now"] = ("Начинаю перерыв", "Starting break"),

        ["notification.eyes.title"] = ("Отдохните от экрана!", "Rest your eyes from the screen!"),
        ["notification.eyes.body"] = ("Правило 20-20-20: посмотрите на объект в 6 метрах (20 футов) от вас в течение 20 секунд.", "The 20-20-20 Rule: Look at an object 20 feet (6 meters) away from you for 20 seconds."),
        ["notification.neck.title"] = ("Разминка для шеи", "Neck Stretch"),
        ["notification.neck.body"] = ("Выполните медленные наклоны головы: вперёд-назад, затем к левому и правому плечу. По 5 раз.", "Perform slow head tilts: forward-backward, then to the left and right shoulder. 5 times each."),
        ["notification.back.title"] = ("Встаньте и разомнитесь!", "Stand up and stretch!"),
        ["notification.back.body"] = ("Встаньте, потянитесь вверх, сделайте несколько наклонов в стороны и прогните спину.", "Stand up, stretch upwards, do a few side bends, and arch your back.")
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
