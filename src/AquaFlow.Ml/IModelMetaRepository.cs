namespace AquaFlow.Ml;

/// <summary>Реестр обученных моделей (таблица `model_meta`, ТЗ раздел 7).</summary>
public interface IModelMetaRepository
{
    Task<long> InsertAsync(
        int epochs,
        double subsetAccuracy,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>Последняя обученная модель (для вкладки «Метрики», M6) или null, если обучения ещё не было.</summary>
    Task<ModelMetaInfo?> GetLatestAsync(CancellationToken cancellationToken = default);
}
