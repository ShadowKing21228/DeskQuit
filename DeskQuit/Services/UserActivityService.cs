using System;
using System.Runtime.InteropServices;
using DeskQuit.Services.Logging;

namespace DeskQuit.Services;

/// <summary>
/// Сервис для отслеживания времени бездействия (AFK) пользователя.
/// </summary>
public static class UserActivityService
{
    // Структура для получения информации о последнем вводе (Windows)
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    /// <summary>
    /// Возвращает время простоя (бездействия мыши и клавиатуры) в секундах.
    /// Поддерживается только на ОС Windows. 
    /// На других ОС всегда возвращает 0.
    /// </summary>
    public static TimeSpan GetIdleTime()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Для Linux/macOS пока возвращаем 0 (или можно реализовать специфичные вызовы)
            return TimeSpan.Zero;
        }

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
}
