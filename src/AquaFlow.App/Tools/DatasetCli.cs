using AquaFlow.Core;
using AquaFlow.Ml;

namespace AquaFlow.App.Tools;

/// <summary>
/// Консольный сценарий M3: применяет миграции, генерирует полный датасет
/// и сохраняет его в Postgres. Запуск: `dotnet run --project src/AquaFlow.App -- --generate-dataset`.
/// GUI при этом не открывается — только вывод прогресса в консоль.
/// </summary>
internal static class DatasetCli
{
    public static async Task<int> RunAsync()
    {
        try
        {
            var connectionString = AquaFlowConnectionString.Resolve();

            Console.WriteLine("Применяю миграции схемы БД...");
            await SchemaMigrator.ApplyAsync(connectionString);

            Console.WriteLine("Генерирую полный датасет (3 входа x 2^7 клапанов = 384 конфигурации)...");
            IPipeSimulator simulator = new PipeSimulator();
            IDatasetGenerator generator = new DatasetGenerator(simulator);
            var samples = generator.GenerateFullDataset();
            Console.WriteLine($"  Сгенерировано записей: {samples.Count}.");

            var split = TrainTestSplitter.Split(samples);
            Console.WriteLine(
                $"  Разбиение train/test: {split.Train.Count}/{split.Test.Count} " +
                $"(seed={TrainTestSplitter.DefaultSeed}, воспроизводимо).");

            Console.WriteLine("Записываю датасет в таблицу samples...");
            ISampleRepository repository = new PostgresSampleRepository(connectionString);
            await repository.ReplaceAllAsync(samples);

            var stored = await repository.GetAllAsync();
            Console.WriteLine($"Готово: в таблице samples теперь {stored.Count} строк.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка генерации датасета: {ex.Message}");
            return 1;
        }
    }
}
