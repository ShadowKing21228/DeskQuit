using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DeskQuit.Services.Logging;

namespace DeskQuit.Services;

/// <summary>
/// Сервис для отслеживания времени бездействия (AFK) пользователя.
/// </summary>
public static class UserActivityService
{
#if WINDOWS
    // Структура для получения информации о последнем вводе (Windows)
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
#endif

    /// <summary>
    /// Возвращает время простоя (бездействия мыши и клавиатуры).
    /// Поддерживается на Windows (через Win32 API) и Linux (через xprintidle).
    /// </summary>
    public static TimeSpan GetIdleTime()
    {
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            return GetWindowsIdleTime();
        }
#endif
        return OperatingSystem.IsLinux() ? GetLinuxIdleTime() : TimeSpan.Zero;
    }

#if WINDOWS
    private static TimeSpan GetWindowsIdleTime()
    {
        var lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (GetLastInputInfo(ref lastInputInfo))
        {
            // Environment.TickCount получает количество миллисекунд с момента запуска системы.
            // dwTime - это TickCount последнего ввода.
            var elapsedTicks = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return TimeSpan.FromMilliseconds(elapsedTicks);
        }

        AppLogger.Warning("GetLastInputInfo failed.", nameof(UserActivityService));
        return TimeSpan.Zero;
    }
#endif

    private static TimeSpan GetLinuxIdleTime()
    {
        try
        {
            // Используем утилиту xprintidle (стандарт для определения AFK в X11)
            var processInfo = new ProcessStartInfo
            {
                FileName = "xprintidle",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1000); // Тайм-аут 1 сек, чтобы не блокировать поток

                if (process.ExitCode == 0 && int.TryParse(output.Trim(), out var idleMilliseconds))
                {
                    return TimeSpan.FromMilliseconds(idleMilliseconds);
                }
            }
        }
        catch (Exception ex)
        {
            // xprintidle может быть не установлен на целевой машине. 
            // Логируем это один раз, но не спамим (или спамим, если нужно отладить).
            AppLogger.Warning($"Failed to get idle time on Linux (is xprintidle installed?): {ex.Message}", nameof(UserActivityService));
        }

        return TimeSpan.Zero;
    }
}
