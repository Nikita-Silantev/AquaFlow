using System.Diagnostics;
using AquaFlow.Core;
using AquaFlow.Ml;

namespace AquaFlow.App.Tools;

/// <summary>
/// Консольный сценарий M4: читает датасет из Postgres, обучает MLP, сохраняет веса
/// в models/water_mlp.bin, пишет метаданные в model_meta и печатает метрики.
/// Запуск: `dotnet run --project src/AquaFlow.App -- --train-model`.
/// </summary>
internal static class TrainModelCli
{
    public static async Task<int> RunAsync()
    {
        try
        {
            var connectionString = AquaFlowConnectionString.Resolve();
            await SchemaMigrator.ApplyAsync(connectionString);

            Console.WriteLine("Загружаю датасет из таблицы samples...");
            ISampleRepository sampleRepository = new PostgresSampleRepository(connectionString);
            var samples = await sampleRepository.GetAllAsync();

            if (samples.Count == 0)
            {
                Console.WriteLine(
                    "Таблица samples пуста. Сначала выполните: " +
                    "dotnet run --project src/AquaFlow.App -- --generate-dataset");
                return 1;
            }

            Console.WriteLine($"  Загружено записей: {samples.Count}.");

            var split = TrainTestSplitter.Split(samples);
            Console.WriteLine(
                $"  train/test: {split.Train.Count}/{split.Test.Count} " +
                $"(seed={TrainTestSplitter.DefaultSeed}, воспроизводимо).");

            var modelFilePath = ResolveModelFilePath();
            Console.WriteLine($"Обучаю MLP (10→32→16→3), веса сохранятся в: {modelFilePath}");

            var trainer = new ModelTrainer();
            var stopwatch = Stopwatch.StartNew();
            var result = trainer.Train(split, modelFilePath);
            stopwatch.Stop();

            Console.WriteLine($"Обучение заняло {stopwatch.Elapsed.TotalSeconds:F1} с, эпох: {result.Epochs}.");
            PrintMetrics(result.TestMetrics);

            if (result.TestMetrics.SubsetAccuracy < 0.95)
            {
                Console.WriteLine(
                    "Внимание: subset accuracy ниже целевого порога 0.95 из ТЗ (раздел 6.4). " +
                    "Можно увеличить число эпох/ширину скрытого слоя.");
            }

            IModelMetaRepository metaRepository = new PostgresModelMetaRepository(connectionString);
            var metaId = await metaRepository.InsertAsync(
                result.Epochs, result.TestMetrics.SubsetAccuracy, result.ModelFilePath);
            Console.WriteLine($"Метаданные модели записаны в model_meta (id={metaId}).");

            RunSanityCheck(modelFilePath);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка обучения модели: {ex.Message}");
            return 1;
        }
    }

    private static void PrintMetrics(EvaluationMetrics metrics)
    {
        Console.WriteLine($"Subset accuracy (тест): {metrics.SubsetAccuracy:P1}");
        foreach (var (receiver, accuracy) in metrics.PerLabelAccuracy)
        {
            var confusion = metrics.ConfusionMatrices[receiver];
            Console.WriteLine(
                $"  {receiver}: accuracy={accuracy:P1}, " +
                $"TP={confusion.TruePositive} FP={confusion.FalsePositive} " +
                $"FN={confusion.FalseNegative} TN={confusion.TrueNegative}");
        }
    }

    /// <summary>Быстрая проверка, что Predict реально работает поверх сохранённого файла весов.</summary>
    private static void RunSanityCheck(string modelFilePath)
    {
        Console.WriteLine("Проверка Predict на конфигурации (A, все клапаны 0)...");

        var valves = Enum.GetValues<Junction>().ToDictionary(j => j, _ => 0);
        var config = SimConfig.Create(Source.A, valves);

        IWaterPredictor predictor = new TorchWaterPredictor(modelFilePath);
        var prediction = predictor.Predict(config);

        var probsText = string.Join(", ", prediction.Probabilities.Select(kv => $"{kv.Key}={kv.Value:F2}"));
        var predictedText = prediction.PredictedReceivers.Count > 0
            ? string.Join(", ", prediction.PredictedReceivers)
            : "нет";

        Console.WriteLine($"  Вероятности: {probsText}");
        Console.WriteLine($"  Предсказанные приёмники: {predictedText}");
    }

    /// <summary>Находит корень репозитория (по AquaFlow.sln) независимо от рабочей директории процесса.</summary>
    private static string ResolveModelFilePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AquaFlow.sln")))
        {
            directory = directory.Parent;
        }

        var repoRoot = directory?.FullName
            ?? throw new InvalidOperationException(
                "Не удалось найти корень репозитория (AquaFlow.sln) для сохранения модели.");

        return Path.Combine(repoRoot, "models", "water_mlp.bin");
    }
}
