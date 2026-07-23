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
}
