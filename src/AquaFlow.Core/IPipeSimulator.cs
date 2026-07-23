namespace AquaFlow.Core;

/// <summary>
/// Детерминированный симулятор сети труб.
/// Одинаковый <see cref="SimConfig"/> всегда даёт одинаковый <see cref="SimulationResult"/>.
/// </summary>
public interface IPipeSimulator
{
    /// <summary>Прогоняет воду по сети согласно конфигурации и возвращает результат.</summary>
    SimulationResult Run(SimConfig config);
}
