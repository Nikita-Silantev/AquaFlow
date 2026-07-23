using System.Reflection;
using Npgsql;

namespace AquaFlow.Ml;

/// <summary>
/// Применяет SQL-миграции из db/migrations (встроены в сборку как EmbeddedResource,
/// см. AquaFlow.Ml.csproj) — не зависит от рабочей директории процесса.
/// </summary>
public static class SchemaMigrator
{
    public static async Task ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var migrationResourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var resourceName in migrationResourceNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Не найден встроенный ресурс миграции: {resourceName}");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
