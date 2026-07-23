using AquaFlow.Core;

namespace AquaFlow.Ml;

/// <summary>
/// Результат работы нейросети для одной конфигурации (ТЗ, раздел 6.2).
/// </summary>
/// <param name="Probabilities">Сырые вероятности сигмоиды (0..1) по каждому приёмнику.</param>
/// <param name="PredictedReceivers">Приёмники с вероятностью ≥ 0.5.</param>
public sealed record Prediction(
    IReadOnlyDictionary<Receiver, float> Probabilities,
    IReadOnlySet<Receiver> PredictedReceivers);
