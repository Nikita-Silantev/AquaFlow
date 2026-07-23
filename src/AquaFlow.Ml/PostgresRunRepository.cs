using System.Text.Json;
using Dapper;
using Npgsql;

namespace AquaFlow.Ml;

/// <summary>Реализация <see cref="IRunRepository"/> поверх Npgsql + Dapper.</summary>
public sealed class PostgresRunRepository : IRunRepository
{
    private readonly string _connectionString;

    public PostgresRunRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task InsertAsync(RunRecord run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Явные касты ::text[] / ::jsonb нужны, чтобы Npgsql корректно определял тип
        // параметра даже когда значение — NULL (предсказания могло не быть).
        const string sql = """
            INSERT INTO runs (mode, source, valves, predicted_receivers, predicted_probs, actual_receivers, was_correct)
            VALUES (@Mode, @Source, @Valves::jsonb, @PredictedReceivers::text[], @PredictedProbs::jsonb,
                    @ActualReceivers::text[], @WasCorrect);
            """;

        var valvesJson = JsonSerializer.Serialize(run.Valves.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
        var predictedReceivers = run.PredictedReceivers?.Select(r => r.ToString()).ToArray();
        var predictedProbsJson = run.PredictedProbabilities is null
            ? null
            : JsonSerializer.Serialize(run.PredictedProbabilities.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
        var actualReceivers = run.ActualReceivers.Select(r => r.ToString()).ToArray();

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            run.Mode,
            Source = run.Source.ToString(),
            Valves = valvesJson,
            PredictedReceivers = predictedReceivers,
            PredictedProbs = predictedProbsJson,
            ActualReceivers = actualReceivers,
            run.WasCorrect
        }, cancellationToken: cancellationToken));
    }

    public async Task<RunAccuracySummary> GetAccuracySummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                count(*) FILTER (WHERE was_correct IS NOT NULL) AS Total,
                count(*) FILTER (WHERE was_correct) AS Correct
            FROM runs;
            """;

        var row = await connection.QuerySingleAsync<AccuracyRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return new RunAccuracySummary(row.Total, row.Correct);
    }

    private sealed class AccuracyRow
    {
        public int Total { get; set; }
        public int Correct { get; set; }
    }
}
