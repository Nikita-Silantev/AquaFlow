namespace AquaFlow.Core;

/// <summary>
/// Конфигурация прогона: выбранный вход + состояния всех 7 клапанов.
/// Полностью определяет результат симуляции.
/// </summary>
public sealed record SimConfig
{
    /// <summary>Выбранный вход (A, B или C).</summary>
    public Source Source { get; }

    /// <summary>Состояния клапанов узлов J1..J7 (значение 0 или 1).</summary>
    public IReadOnlyDictionary<Junction, int> Valves { get; }

    private SimConfig(Source source, IReadOnlyDictionary<Junction, int> valves)
    {
        Source = source;
        Valves = valves;
    }

    /// <summary>
    /// Создаёт конфигурацию с валидацией: для каждого из семи узлов должно быть
    /// задано значение клапана 0 либо 1.
    /// </summary>
    public static SimConfig Create(Source source, IReadOnlyDictionary<Junction, int> valves)
    {
        ArgumentNullException.ThrowIfNull(valves);

        foreach (var junction in Enum.GetValues<Junction>())
        {
            if (!valves.TryGetValue(junction, out var value) || (value != 0 && value != 1))
            {
                throw new ArgumentException(
                    $"Клапан узла {junction} должен быть задан значением 0 или 1.", nameof(valves));
            }
        }

        return new SimConfig(source, new Dictionary<Junction, int>(valves));
    }
}
