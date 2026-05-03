using System;
using System.Diagnostics;
using System.IO;

namespace DeskQuit.Services.Logging;

public static class AppLogger
{
    private static bool _isConfigured;

    public static void Configure()
    {
        if (_isConfigured)
            return;

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskQuit",
            "logs");

        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, $"deskquit-{DateTime.Now:yyyyMMdd}.log");
        Trace.AutoFlush = true;
        Trace.Listeners.Add(new TextWriterTraceListener(logFilePath, "deskquit-file"));

        _isConfigured = true;
        Info("Logger configured", nameof(AppLogger));
    }

    public static void Info(string message, string source = "App")
        => Write("INFO", source, message);

    public static void Warning(string message, string source = "App")
        => Write("WARN", source, message);

    public static void Error(string message, string source = "App")
        => Write("ERROR", source, message);

    public static void Error(Exception exception, string message, string source = "App")
        => Write("ERROR", source, $"{message}. Exception: {exception}");

    private static void Write(string level, string source, string message)
    {
        var line = $"{DateTime.Now:O} [{level}] [{source}] {message}";
        Trace.WriteLine(line);
    }
}
