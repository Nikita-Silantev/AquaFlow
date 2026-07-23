namespace AquaFlow.Ml;

/// <summary>Генератор полного размеченного датасета по всем возможным конфигурациям.</summary>
public interface IDatasetGenerator
{
    /// <summary>
    /// Прогоняет симулятор по всем 3 × 2⁷ = 384 конфигурациям (вход × состояния 7 клапанов)
    /// и возвращает размеченные записи.
    /// </summary>
    IReadOnlyList<Sample> GenerateFullDataset();
}
