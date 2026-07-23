using Avalonia;
using AquaFlow.App.Tools;

namespace AquaFlow.App;

/// <summary>
/// Точка входа приложения AquaFlow.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Обычный запуск открывает GUI. Флаг --generate-dataset вместо этого прогоняет
    /// консольный сценарий M3 (миграции + генерация датасета + запись в Postgres)
    /// и завершает процесс без показа окна.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--generate-dataset"))
        {
            return DatasetCli.RunAsync().GetAwaiter().GetResult();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>
    /// Настройка Avalonia: автоопределение платформы (для запуска нативно под macOS arm64).
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
