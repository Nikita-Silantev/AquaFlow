using static TorchSharp.torch;

namespace AquaFlow.Ml;

/// <summary>Итог обучения: сколько эпох, метрики на отложенном тесте, путь к файлу весов.</summary>
public sealed record TrainingResult(int Epochs, EvaluationMetrics TestMetrics, string ModelFilePath);

/// <summary>
/// Обучает <see cref="WaterMlpModel"/> на train-выборке (ТЗ, раздел 6.3):
/// Adam, BCELoss, ~200-500 эпох, батч ~64. Тест держится отложенным для честной оценки.
/// </summary>
public sealed class ModelTrainer
{
    /// <summary>
    /// Seed для перемешивания батчей между эпохами — фиксирован, чтобы обучение
    /// было воспроизводимо наравне с train/test split'ом.
    /// </summary>
    private const int ShuffleSeed = 1234;

    public TrainingResult Train(
        DatasetSplit split,
        string modelFilePath,
        int epochs = 2000,
        int batchSize = 64,
        double learningRate = 1e-3)
    {
        ArgumentNullException.ThrowIfNull(split);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelFilePath);

        var model = new WaterMlpModel();
        model.train();

        using var optimizer = optim.Adam(model.parameters(), lr: learningRate);
        using var lossFunction = nn.BCELoss();

        var (trainFeatures, trainLabels) = ToTensors(split.Train);
        var sampleCount = split.Train.Count;

        // Датасет сгенерирован по порядку (все конфигурации A, потом B, потом C),
        // поэтому без перемешивания батчи внутри эпохи сильно коррелированы —
        // это и было причиной заниженного subset accuracy. Перемешиваем индексы
        // перед каждой эпохой детерминированным (по фиксированному seed) генератором.
        var random = new Random(ShuffleSeed);
        var indices = new long[sampleCount];

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            ShuffleIndices(indices, random);
            using var indexTensor = tensor(indices);
            using var shuffledFeatures = trainFeatures.index_select(0, indexTensor);
            using var shuffledLabels = trainLabels.index_select(0, indexTensor);

            for (var start = 0; start < sampleCount; start += batchSize)
            {
                var length = Math.Min(batchSize, sampleCount - start);
                using var batchFeatures = shuffledFeatures.narrow(0, start, length);
                using var batchLabels = shuffledLabels.narrow(0, start, length);

                optimizer.zero_grad();
                using var predictions = model.forward(batchFeatures);
                using var loss = lossFunction.forward(predictions, batchLabels);
                loss.backward();
                optimizer.step();
            }
        }

        model.eval();

        var modelDirectory = Path.GetDirectoryName(modelFilePath);
        if (!string.IsNullOrEmpty(modelDirectory))
        {
            Directory.CreateDirectory(modelDirectory);
        }

        model.save(modelFilePath);

        var testMetrics = ModelEvaluator.Evaluate(model, split.Test);

        return new TrainingResult(epochs, testMetrics, modelFilePath);
    }

    private static (Tensor Features, Tensor Labels) ToTensors(IReadOnlyList<Sample> samples)
    {
        var count = samples.Count;
        var featuresFlat = new float[count * FeatureEncoder.FeatureCount];
        var labelsFlat = new float[count * FeatureEncoder.LabelCount];

        for (var i = 0; i < count; i++)
        {
            var features = FeatureEncoder.EncodeFeatures(samples[i]);
            Array.Copy(features, 0, featuresFlat, i * FeatureEncoder.FeatureCount, FeatureEncoder.FeatureCount);

            var labels = FeatureEncoder.EncodeLabels(samples[i].ReachedReceivers);
            Array.Copy(labels, 0, labelsFlat, i * FeatureEncoder.LabelCount, FeatureEncoder.LabelCount);
        }

        var featuresTensor = tensor(featuresFlat).reshape(count, FeatureEncoder.FeatureCount);
        var labelsTensor = tensor(labelsFlat).reshape(count, FeatureEncoder.LabelCount);

        return (featuresTensor, labelsTensor);
    }

    /// <summary>Заполняет индексы 0..N-1 и перемешивает их (Fisher-Yates) на месте.</summary>
    private static void ShuffleIndices(long[] indices, Random random)
    {
        for (var i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        for (var i = indices.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
    }
}
