namespace AquaFlow.Ml;

/// <summary>
/// Строка подключения к Postgres берётся из переменной окружения, не хардкодится
/// (ТЗ, раздел 11). Значение по умолчанию совпадает с учётными данными из
/// docker-compose.yml — удобно для локальной разработки без дополнительной настройки.
/// </summary>
public static class AquaFlowConnectionString
{
    public const string EnvironmentVariableName = "AQUAFLOW_CONNECTION_STRING";

    private const string LocalDevDefault =
        "Host=localhost;Port=5433;Database=aquaflow;Username=aquaflow;Password=aquaflow";

    public static string Resolve()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        return string.IsNullOrWhiteSpace(fromEnv) ? LocalDevDefault : fromEnv;
    }
}
