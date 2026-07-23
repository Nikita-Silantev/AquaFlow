namespace AquaFlow.Ml;

/// <summary>
/// Воспроизводимое разбиение датасета на train/test с фиксированным seed (ТЗ, раздел 6.3).
/// Тест держится отложенным — на нём в M4/M6 показывается, что сеть обобщает, а не заучивает таблицу.
/// </summary>
public static class TrainTestSplitter
{
    /// <summary>Seed по умолчанию — при неизменном входном датасете разбиение всегда одинаково.</summary>
    public const int DefaultSeed = 42;

    public static DatasetSplit Split(IReadOnlyList<Sample> samples, double trainRatio = 0.7, int seed = DefaultSeed)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (trainRatio is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(trainRatio), "Доля train должна быть в диапазоне (0, 1).");
        }

        // Детерминированная перестановка индексов (Fisher-Yates) от фиксированного seed —
        // не зависит от порядка выполнения или хеширования, только от содержимого samples и seed.
        var indices = Enumerable.Range(0, samples.Count).ToList();
        var random = new Random(seed);
        for (var i = indices.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var trainCount = (int)Math.Round(samples.Count * trainRatio);
        var train = indices.Take(trainCount).Select(i => samples[i]).ToList();
        var test = indices.Skip(trainCount).Select(i => samples[i]).ToList();

        return new DatasetSplit(train, test);
    }
}
