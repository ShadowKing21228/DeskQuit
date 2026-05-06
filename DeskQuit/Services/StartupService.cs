using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DeskQuit.Services.Logging;

namespace DeskQuit.Services;

public static class StartupService
{
    private const string AppName = "DeskQuit";

    public static void SetRunOnStartup(bool enable)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetRunOnStartupWindows(enable);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SetRunOnStartupLinux(enable);
            }
            else
            {
                AppLogger.Info("Startup is not supported on this OS platform.", nameof(StartupService));
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to set run on startup: {ex.Message}", nameof(StartupService));
        }
    }

    public static bool GetRunOnStartup()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetRunOnStartupWindows();
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetRunOnStartupLinux();
            }
            
            return false;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to get run on startup status: {ex.Message}", nameof(StartupService));
            return false;
        }
    }

    private static void SetRunOnStartupWindows(bool enable)
    {
        // Use a background process to avoid adding a reference to Microsoft.Win32.Registry in an Avalonia project
        var processPath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(processPath)) return;

        var command = enable
            ? $"Add-MpPreference -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{AppName}' -Value '\"\"{processPath}\"\"'"
            : $"Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{AppName}' -ErrorAction SilentlyContinue";

        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        Process.Start(processInfo)?.WaitForExit();
    }

    private static bool GetRunOnStartupWindows()
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"(Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{AppName}' -ErrorAction SilentlyContinue).{AppName}\"",
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processInfo);
        var output = process?.StandardOutput.ReadToEnd();
        process?.WaitForExit();

        return !string.IsNullOrWhiteSpace(output);
    }

    private static void SetRunOnStartupLinux(bool enable)
    {
        var processPath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(processPath)) return;

        var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
        var desktopFilePath = Path.Combine(autostartDir, $"{AppName.ToLower()}.desktop");

        if (enable)
        {
            Directory.CreateDirectory(autostartDir);
            var content = $"""
                           [Desktop Entry]
                           Type=Application
                           Exec={processPath}
                           Hidden=false
                           NoDisplay=false
                           X-GNOME-Autostart-enabled=true
                           Name={AppName}
                           Comment=Health tracker for PC
                           """;
            File.WriteAllText(desktopFilePath, content);
        }
        else
        {
            if (File.Exists(desktopFilePath))
            {
                File.Delete(desktopFilePath);
            }
        }
    }

    private static bool GetRunOnStartupLinux()
    {
        var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
        var desktopFilePath = Path.Combine(autostartDir, $"{AppName.ToLower()}.desktop");
        return File.Exists(desktopFilePath);
    }
}
