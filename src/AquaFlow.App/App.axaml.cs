using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AquaFlow.App.Tools;
using AquaFlow.Ml;

namespace AquaFlow.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var connectionString = AquaFlowConnectionString.Resolve();
            ISampleRepository sampleRepository = new PostgresSampleRepository(connectionString);
            IRunRepository runRepository = new PostgresRunRepository(connectionString);
            IModelMetaRepository modelMetaRepository = new PostgresModelMetaRepository(connectionString);

            // Модель грузится один раз при старте (ТЗ, раздел 3.1). Если она ещё не
            // обучена (--train-model не выполнялся), приложение всё равно открывается —
            // кнопка «Расчёт» и метрики на тесте просто будут недоступны.
            IWaterPredictor? predictor = null;
            try
            {
                predictor = new TorchWaterPredictor(RepoPaths.ModelFilePath());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Модель не загружена (можно обучить: --train-model): {ex.Message}");
            }

            var services = new AppServices(
                predictor, sampleRepository, runRepository, modelMetaRepository, new LastPredictionState());

            desktop.MainWindow = new MainWindow(services);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
