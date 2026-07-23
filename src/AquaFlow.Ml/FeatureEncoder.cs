using AquaFlow.Core;

namespace AquaFlow.Ml;

/// <summary>
/// Кодирует конфигурацию/результат симулятора в числовые векторы для нейросети
/// (ТЗ, раздел 6.1): 10 признаков (one-hot входа + 7 клапанов), 3 метки (X, Y, Z).
/// </summary>
public static class FeatureEncoder
{
    public const int FeatureCount = 10;
    public const int LabelCount = 3;

    /// <summary>Порядок клапанов в векторе признаков — J1..J7 (фиксирован, важен для обучения и инференса).</summary>
    private static readonly Junction[] JunctionOrder = Enum.GetValues<Junction>();

    /// <summary>Порядок приёмников в векторе меток — X, Y, Z.</summary>
    private static readonly Receiver[] ReceiverOrder = Enum.GetValues<Receiver>();

    public static float[] EncodeFeatures(Source source, IReadOnlyDictionary<Junction, int> valves)
    {
        var features = new float[FeatureCount];
        features[0] = source == Source.A ? 1f : 0f;
        features[1] = source == Source.B ? 1f : 0f;
        features[2] = source == Source.C ? 1f : 0f;

        for (var i = 0; i < JunctionOrder.Length; i++)
        {
            features[3 + i] = valves[JunctionOrder[i]];
        }

        return features;
    }

    public static float[] EncodeFeatures(Sample sample) => EncodeFeatures(sample.Source, sample.Valves);

    public static float[] EncodeLabels(IReadOnlySet<Receiver> reachedReceivers)
    {
        var labels = new float[LabelCount];
        for (var i = 0; i < ReceiverOrder.Length; i++)
        {
            labels[i] = reachedReceivers.Contains(ReceiverOrder[i]) ? 1f : 0f;
        }

        return labels;
    }

    /// <summary>Порядок приёмников, соответствующий индексам в выходном векторе модели.</summary>
    public static IReadOnlyList<Receiver> ReceiversInOrder => ReceiverOrder;
}
