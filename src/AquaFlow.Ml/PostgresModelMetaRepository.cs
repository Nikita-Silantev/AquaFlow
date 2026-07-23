using Dapper;
using Npgsql;

namespace AquaFlow.Ml;

/// <summary>Реализация <see cref="IModelMetaRepository"/> поверх Npgsql + Dapper.</summary>
public sealed class PostgresModelMetaRepository : IModelMetaRepository
{
    private readonly string _connectionString;

    public PostgresModelMetaRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<long> InsertAsync(
        int epochs,
        double subsetAccuracy,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO model_meta (epochs, subset_accuracy, file_path)
            VALUES (@Epochs, @SubsetAccuracy, @FilePath)
            RETURNING id;
            """;

        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new { Epochs = epochs, SubsetAccuracy = subsetAccuracy, FilePath = filePath },
            cancellationToken: cancellationToken));
    }

    public async Task<ModelMetaInfo?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id, trained_at AS TrainedAt, epochs AS Epochs,
                   subset_accuracy AS SubsetAccuracy, file_path AS FilePath
            FROM model_meta
            ORDER BY id DESC
            LIMIT 1;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<ModelMetaRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new ModelMetaInfo(row.Id, row.TrainedAt, row.Epochs, row.SubsetAccuracy, row.FilePath);
    }

    /// <summary>Плоское представление строки для Dapper (обычный класс — см. пояснение в PostgresSampleRepository).</summary>
    private sealed class ModelMetaRow
    {
        public long Id { get; set; }
        public DateTimeOffset TrainedAt { get; set; }
        public int Epochs { get; set; }
        public double SubsetAccuracy { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}
