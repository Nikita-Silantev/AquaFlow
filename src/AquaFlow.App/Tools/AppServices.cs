using AquaFlow.Ml;

namespace AquaFlow.App.Tools;

/// <summary>
/// Зависимости, собранные один раз при старте приложения (композиция в App.axaml.cs)
/// и раздаваемые всем трём вкладкам MainWindow.
/// </summary>
/// <param name="Predictor">
/// Загруженная модель или null, если она ещё не обучена (--train-model не выполнялся).
/// </param>
public sealed record AppServices(
    IWaterPredictor? Predictor,
    ISampleRepository SampleRepository,
    IRunRepository RunRepository,
    IModelMetaRepository ModelMetaRepository,
    LastPredictionState LastPredictionState);
