namespace AquaFlow.Ml;

/// <summary>Доступ к таблице `samples` в Postgres.</summary>
public interface ISampleRepository
{
    /// <summary>
    /// Полностью заменяет содержимое таблицы новым датасетом (используется при регенерации).
    /// </summary>
    Task ReplaceAllAsync(IReadOnlyList<Sample> samples, CancellationToken cancellationToken = default);

    /// <summary>Читает весь датасет из таблицы (например, для обучения в M4).</summary>
    Task<IReadOnlyList<Sample>> GetAllAsync(CancellationToken cancellationToken = default);
}
