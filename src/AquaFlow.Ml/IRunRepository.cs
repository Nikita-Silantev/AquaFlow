namespace AquaFlow.Ml;

/// <summary>Доступ к таблице `runs` — истории прогонов и живому счётчику точности.</summary>
public interface IRunRepository
{
    Task InsertAsync(RunRecord run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Считает, сколько прогонов сравнивались с предсказанием и сколько из них совпали —
    /// используется для восстановления счётчика точности после перезапуска приложения.
    /// </summary>
    Task<RunAccuracySummary> GetAccuracySummaryAsync(CancellationToken cancellationToken = default);
}
