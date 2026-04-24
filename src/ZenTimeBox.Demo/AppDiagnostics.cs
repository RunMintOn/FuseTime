using System.Globalization;

namespace ZenTimeBox.Demo;

internal static class AppDiagnostics
{
    private static readonly object Gate = new();
    private static readonly string LogFilePath = BuildLogPath();

    public static void LogInfo(string message)
    {
        WriteLine($"INFO {message}");
    }

    public static void LogException(Exception exception, string context)
    {
        WriteLine($"ERROR {context}{Environment.NewLine}{exception}");
    }

    private static void WriteLine(string message)
    {
        try
        {
            string line = $"[{DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {message}{Environment.NewLine}";
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch
        {
            // Logging should never take the app down.
        }
    }

    private static string BuildLogPath()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZenTimeBox");
        return Path.Combine(root, "logs", "app.log");
    }
}
