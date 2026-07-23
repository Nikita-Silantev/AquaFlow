using System.Text.Json;
using AquaFlow.Core;
using Dapper;
using Npgsql;

namespace AquaFlow.Ml;

/// <summary>Реализация <see cref="ISampleRepository"/> поверх Npgsql + Dapper.</summary>
public sealed class PostgresSampleRepository : ISampleRepository
{
    private readonly string _connectionString;

    public PostgresSampleRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task ReplaceAllAsync(IReadOnlyList<Sample> samples, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Полная регенерация: датасет детерминирован, поэтому перезаписываем таблицу целиком.
        await connection.ExecuteAsync(
            new CommandDefinition("TRUNCATE TABLE samples RESTART IDENTITY;", transaction: transaction,
                cancellationToken: cancellationToken));

        const string insertSql = """
            INSERT INTO samples (source, valves, reached_receivers)
            VALUES (@Source, @Valves::jsonb, @ReachedReceivers);
            """;

        foreach (var sample in samples)
        {
            var row = ToRow(sample);
            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, row, transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sample>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string selectSql = """
            SELECT source AS Source, valves::text AS Valves, reached_receivers AS ReachedReceivers
            FROM samples
            ORDER BY id;
            """;

        var rows = await connection.QueryAsync<SampleRow>(
            new CommandDefinition(selectSql, cancellationToken: cancellationToken));

        return rows.Select(FromRow).ToList();
    }

    /// <summary>
    /// Плоское представление строки таблицы `samples` для Dapper.
    /// Намеренно обычный класс с сеттерами, а не record: при материализации из БД
    /// Dapper заполняет свойства через сеттеры и сам приводит типы (например,
    /// TEXT[] от Npgsql может прийти как System.Array) — с record'ом и строгим
    /// сопоставлением конструктора по типам параметров это падало с ошибкой маппинга.
    /// </summary>
    private sealed class SampleRow
    {
        public string Source { get; set; } = string.Empty;
        public string Valves { get; set; } = string.Empty;
        public string[] ReachedReceivers { get; set; } = Array.Empty<string>();
    }

    private static SampleRow ToRow(Sample sample)
    {
        var valvesJson = JsonSerializer.Serialize(
            sample.Valves.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
        var receivers = sample.ReachedReceivers.Select(r => r.ToString()).ToArray();

        return new SampleRow
        {
            Source = sample.Source.ToString(),
            Valves = valvesJson,
            ReachedReceivers = receivers
        };
    }

    private static Sample FromRow(SampleRow row)
    {
        var rawValves = JsonSerializer.Deserialize<Dictionary<string, int>>(row.Valves)
            ?? throw new InvalidOperationException("Не удалось разобрать поле valves из БД.");

        var valves = rawValves.ToDictionary(kv => Enum.Parse<Junction>(kv.Key), kv => kv.Value);
        var receivers = row.ReachedReceivers.Select(Enum.Parse<Receiver>).ToHashSet();
        var source = Enum.Parse<Source>(row.Source);

        return new Sample(source, valves, receivers);
    }
}
