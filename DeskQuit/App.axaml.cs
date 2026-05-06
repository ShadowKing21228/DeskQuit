using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DeskQuit.Services;
using DeskQuit.Services.Localization;
using DeskQuit.Services.Logging;
using DeskQuit.Services.Notification;
using DeskQuit.ViewModels;
using DeskQuit.Views;

namespace DeskQuit;

public partial class App : Application
{
    private readonly LocalizationService _localizationService = LocalizationService.Instance;
    private NotificationService? _notificationService;
    private ApiService? _apiService;
    private MainWindowViewModel? _mainWindowViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            
            AppLogger.Configure();
            
            _notificationService = new NotificationService();
            _apiService = new ApiService();
            
            _localizationService.SetLanguage(_localizationService.DetectSystemLanguage());
            _localizationService.LanguageChanged += _ => ApplyLocalizationToTrayMenu();
            _notificationService.Initialize();
            
            _mainWindowViewModel = new MainWindowViewModel(_notificationService, _apiService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel,
            };
             desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
             ApplyLocalizationToTrayMenu();

             AppLogger.Info("Subscribing to ShutdownRequested event", nameof(App));
             desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        AppLogger.Info("OnShutdownRequested called", nameof(App));
        
        if (_notificationService == null)
        {
            AppLogger.Warning("NotificationService is null", nameof(App));
            return;
        }
        
        if (_apiService == null)
        {
            AppLogger.Warning("ApiService is null", nameof(App));
            return;
        }
        
        if (_mainWindowViewModel == null)
        {
            AppLogger.Warning("MainWindowViewModel is null", nameof(App));
            return;
        }
        
        AppLogger.Info($"IsAuthenticated: {_mainWindowViewModel.AccountViewModel.IsAuthenticated}", nameof(App));
        
        if (_mainWindowViewModel.AccountViewModel.IsAuthenticated)
        {
            try
            {
                var (activeSeconds, afkSeconds, notifsTotal, notifsCustom) = _notificationService.GetAndResetStats();
                var dateStr = DateTime.Today.ToString("yyyy-MM-dd");
                AppLogger.Info($"Sending daily stats: active={activeSeconds}s, afk={afkSeconds}s, total_notifs={notifsTotal}, custom_notifs={notifsCustom}", nameof(App));
                
                e.Cancel = true;
                var success = await _apiService.SendDailyStatsAsync(dateStr, activeSeconds, afkSeconds, notifsTotal, notifsCustom);
                AppLogger.Info($"SendDailyStatsAsync result: {success}", nameof(App));
                
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error sending daily stats: {ex.Message}", nameof(App));
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
        }
        else
        {
            AppLogger.Info("User not authenticated, skipping stats send", nameof(App));
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void MenuOpen_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Show();
        }
    }

      private async void MenuExit_Click(object? sender, EventArgs e)
      {
          AppLogger.Info("MenuExit_Click triggered, initiating shutdown", nameof(App));
          AppLogger.Info($"NotificationService: {_notificationService != null}, ApiService: {_apiService != null}, MainViewModel: {_mainWindowViewModel != null}", nameof(App));
          
          // Отправляем статистику перед выходом
          if (_notificationService != null && _apiService != null && _mainWindowViewModel != null && _mainWindowViewModel.AccountViewModel.IsAuthenticated)
          {
              try
              {
                  AppLogger.Info("Starting to send stats", nameof(App));
                  var (activeSeconds, afkSeconds, notifsTotal, notifsCustom) = _notificationService.GetAndResetStats();
                  var dateStr = DateTime.Today.ToString("yyyy-MM-dd");
                  AppLogger.Info($"Sending daily stats: active={activeSeconds}s, afk={afkSeconds}s, total_notifs={notifsTotal}, custom_notifs={notifsCustom}", nameof(App));
                  
                  var success = await _apiService.SendDailyStatsAsync(dateStr, activeSeconds, afkSeconds, notifsTotal, notifsCustom);
                  AppLogger.Info($"SendDailyStatsAsync result: {success}", nameof(App));
                  
                  // Даём время на завершение запроса
                  await Task.Delay(500);
              }
              catch (Exception ex)
              {
                  AppLogger.Error($"Error sending daily stats: {ex.Message}", nameof(App));
              }
          }
          else
          {
              AppLogger.Info("Skipping stats: not authenticated or services null", nameof(App));
          }
          
          AppLogger.Info("About to shutdown", nameof(App));
          if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
          {
              desktop.Shutdown();
          }
      }

    private void MenuLanguageRu_Click(object? sender, EventArgs e)
    {
        _localizationService.SetLanguage(AppLanguage.Russian);
    }

    private void MenuLanguageEn_Click(object? sender, EventArgs e)
    {
        _localizationService.SetLanguage(AppLanguage.English);
    }
    
    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        
        if (desktop.MainWindow == null) return;
        
        var window = desktop.MainWindow;
        
        if (window.IsVisible) {
            window.Hide();
        }
        else {
            desktop.MainWindow.Show();
            desktop.MainWindow.Activate();
        }
    }

    private void ApplyLocalizationToTrayMenu()
    {
        if (!TryGetTrayMenuItems(
                out var trayMainIcon,
                out var openMenuItem,
                out var languageMenuItem,
                out var languageRuMenuItem,
                out var languageEnMenuItem,
                out var exitMenuItem))
        {
            return;
        }

        trayMainIcon.ToolTipText = _localizationService["app.tray.tooltip"];
        openMenuItem.Header = _localizationService["app.tray.open"];
        languageMenuItem.Header = _localizationService["app.tray.language"];
        languageRuMenuItem.Header = _localizationService["app.tray.language.ru"];
        languageEnMenuItem.Header = _localizationService["app.tray.language.en"];
        exitMenuItem.Header = _localizationService["app.tray.exit"];
    }

    private bool TryGetTrayMenuItems(
        out TrayIcon trayMainIcon,
        out NativeMenuItem openMenuItem,
        out NativeMenuItem languageMenuItem,
        out NativeMenuItem languageRuMenuItem,
        out NativeMenuItem languageEnMenuItem,
        out NativeMenuItem exitMenuItem)
    {
        trayMainIcon = null!;
        openMenuItem = null!;
        languageMenuItem = null!;
        languageRuMenuItem = null!;
        languageEnMenuItem = null!;
        exitMenuItem = null!;

        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons is null || trayIcons.Count == 0)
            return false;

        if (trayIcons[0] is not TrayIcon icon)
            return false;

        if (icon.Menu is not NativeMenu rootMenu || rootMenu.Items.Count < 4)
            return false;

        if (rootMenu.Items[0] is not NativeMenuItem openItem)
            return false;

        if (rootMenu.Items[1] is not NativeMenuItem languageItem)
            return false;

        if (languageItem.Menu is not NativeMenu languageMenu || languageMenu.Items.Count < 2)
            return false;

        if (languageMenu.Items[0] is not NativeMenuItem ruItem)
            return false;

        if (languageMenu.Items[1] is not NativeMenuItem enItem)
            return false;

        if (rootMenu.Items[3] is not NativeMenuItem exitItem)
            return false;

        trayMainIcon = icon;
        openMenuItem = openItem;
        languageMenuItem = languageItem;
        languageRuMenuItem = ruItem;
        languageEnMenuItem = enItem;
        exitMenuItem = exitItem;
        return true;
    }
}
