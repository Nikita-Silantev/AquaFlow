using AquaFlow.Core;
using static TorchSharp.torch;

namespace AquaFlow.Ml;

/// <summary>Матрица ошибок для одного приёмника (ТЗ, раздел 6.4).</summary>
public sealed record LabelConfusion(int TruePositive, int FalsePositive, int FalseNegative, int TrueNegative);

/// <summary>Метрики качества модели на выборке (per-label accuracy, subset accuracy, матрицы ошибок).</summary>
public sealed record EvaluationMetrics(
    IReadOnlyDictionary<Receiver, double> PerLabelAccuracy,
    double SubsetAccuracy,
    IReadOnlyDictionary<Receiver, LabelConfusion> ConfusionMatrices);

/// <summary>Считает метрики (ТЗ, раздел 6.4) для модели на заданной выборке.</summary>
public static class ModelEvaluator
{
    /// <summary>
    /// Используется во время обучения (M4, <see cref="ModelTrainer"/>) — работает напрямую
    /// с моделью, которая ещё не сохранена на диск.
    /// </summary>
    public static EvaluationMetrics Evaluate(WaterMlpModel model, IReadOnlyList<Sample> samples)
    {
        model.eval();
        using var _ = no_grad();

        return EvaluateCore(samples, sample =>
        {
            var features = FeatureEncoder.EncodeFeatures(sample);
            var input = tensor(features).reshape(1, FeatureEncoder.FeatureCount);
            var output = model.forward(input).reshape(FeatureEncoder.LabelCount);
            var probs = output.data<float>().ToArray();

            var predicted = new HashSet<Receiver>();
            var receivers = FeatureEncoder.ReceiversInOrder;
            for (var i = 0; i < receivers.Count; i++)
            {
                if (probs[i] >= 0.5f)
                {
                    predicted.Add(receivers[i]);
                }
            }

            return predicted;
        });
    }

    /// <summary>
    /// Используется вкладкой «Метрики» (M6) — работает только через <see cref="IWaterPredictor"/>,
    /// не трогая внутренности модели напрямую (ТЗ, раздел 10: работа с моделью — только через интерфейс).
    /// </summary>
    public static EvaluationMetrics Evaluate(IWaterPredictor predictor, IReadOnlyList<Sample> samples)
    {
        ArgumentNullException.ThrowIfNull(predictor);

        return EvaluateCore(samples, sample =>
        {
            var config = SimConfig.Create(sample.Source, sample.Valves);
            return predictor.Predict(config).PredictedReceivers;
        });
    }

    private static EvaluationMetrics EvaluateCore(
        IReadOnlyList<Sample> samples,
        Func<Sample, IReadOnlySet<Receiver>> predict)
    {
        var receivers = FeatureEncoder.ReceiversInOrder;
        var truePositive = new Dictionary<Receiver, int>();
        var falsePositive = new Dictionary<Receiver, int>();
        var falseNegative = new Dictionary<Receiver, int>();
        var trueNegative = new Dictionary<Receiver, int>();
        foreach (var receiver in receivers)
        {
            truePositive[receiver] = 0;
            falsePositive[receiver] = 0;
            falseNegative[receiver] = 0;
            trueNegative[receiver] = 0;
        }

        var subsetCorrect = 0;

        foreach (var sample in samples)
        {
            var predictedReceivers = predict(sample);

            var allCorrect = true;
            foreach (var receiver in receivers)
            {
                var predicted = predictedReceivers.Contains(receiver);
                var actual = sample.ReachedReceivers.Contains(receiver);

                if (predicted && actual) truePositive[receiver]++;
                else if (predicted && !actual) falsePositive[receiver]++;
                else if (!predicted && actual) falseNegative[receiver]++;
                else trueNegative[receiver]++;

                if (predicted != actual)
                {
                    allCorrect = false;
                }
            }

            if (allCorrect)
            {
                subsetCorrect++;
            }
        }

        var total = samples.Count;
        var perLabelAccuracy = receivers.ToDictionary(
            r => r,
            r => total == 0 ? 0.0 : (double)(truePositive[r] + trueNegative[r]) / total);

        var confusionMatrices = receivers.ToDictionary(
            r => r,
            r => new LabelConfusion(truePositive[r], falsePositive[r], falseNegative[r], trueNegative[r]));

        var subsetAccuracy = total == 0 ? 0.0 : (double)subsetCorrect / total;

        return new EvaluationMetrics(perLabelAccuracy, subsetAccuracy, confusionMatrices);
    }
}
