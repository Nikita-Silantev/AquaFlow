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

    /// <summary>Последние прогоны для вкладки «История» (M6), от новых к старым.</summary>
    Task<IReadOnlyList<RunHistoryEntry>> GetHistoryAsync(
        int limit = 200,
        CancellationToken cancellationToken = default);
}
