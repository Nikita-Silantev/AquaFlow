using Avalonia;

namespace AquaFlow.App;

/// <summary>
/// Точка входа приложения AquaFlow.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Настройка Avalonia: автоопределение платформы (для запуска нативно под macOS arm64).
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
