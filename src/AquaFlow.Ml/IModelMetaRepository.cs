namespace AquaFlow.Ml;

/// <summary>Реестр обученных моделей (таблица `model_meta`, ТЗ раздел 7).</summary>
public interface IModelMetaRepository
{
    Task<long> InsertAsync(
        int epochs,
        double subsetAccuracy,
        string filePath,
        CancellationToken cancellationToken = default);
}
