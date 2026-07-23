using Avalonia;
using AquaFlow.App.Tools;

namespace AquaFlow.App;

/// <summary>
/// Точка входа приложения AquaFlow.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Обычный запуск открывает GUI. Флаги --generate-dataset (M3) и --train-model (M4)
    /// вместо этого прогоняют соответствующий консольный сценарий и завершают процесс
    /// без показа окна.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--generate-dataset"))
        {
            return DatasetCli.RunAsync().GetAwaiter().GetResult();
        }

        if (args.Contains("--train-model"))
        {
            return TrainModelCli.RunAsync().GetAwaiter().GetResult();
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
