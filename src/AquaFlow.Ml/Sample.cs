using AquaFlow.Core;

namespace AquaFlow.Ml;

/// <summary>
/// Одна размеченная запись обучающего датасета: конфигурация прогона + результат симулятора.
/// Симулятор сам себе учитель — метки не размечаются руками (ТЗ, раздел 3.3).
/// </summary>
/// <param name="Source">Выбранный вход.</param>
/// <param name="Valves">Состояния клапанов узлов J1..J7.</param>
/// <param name="ReachedReceivers">Приёмники, до которых дошла вода (истинная метка).</param>
public sealed record Sample(
    Source Source,
    IReadOnlyDictionary<Junction, int> Valves,
    IReadOnlySet<Receiver> ReachedReceivers);
