using AquaFlow.Core;
using AquaFlow.Ml;

namespace AquaFlow.App.Tools;

/// <summary>
/// Разделяемое состояние последнего расчёта нейросети — вкладка «Симуляция» пишет сюда
/// после каждого клика «Расчёт», вкладка «Метрики» читает отсюда для блока «уверенность
/// для последней конфигурации» (ТЗ, раздел 8.3). Простой общий объект вместо шины событий —
/// достаточно для однооконного демо-приложения.
/// </summary>
public sealed class LastPredictionState
{
    public SimConfig? Config { get; private set; }

    public Prediction? Prediction { get; private set; }

    public void Update(SimConfig config, Prediction prediction)
    {
        Config = config;
        Prediction = prediction;
    }
}
