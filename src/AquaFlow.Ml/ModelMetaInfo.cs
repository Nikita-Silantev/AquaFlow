namespace AquaFlow.Ml;

/// <summary>Одна запись реестра обученных моделей (таблица `model_meta`, ТЗ раздел 7).</summary>
public sealed record ModelMetaInfo(
    long Id,
    DateTimeOffset TrainedAt,
    int Epochs,
    double SubsetAccuracy,
    string FilePath);
