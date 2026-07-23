namespace AquaFlow.Core;

/// <summary>
/// Результат одного прогона симулятора.
/// </summary>
/// <param name="ReachedReceivers">Множество приёмников, до которых дошла вода.</param>
/// <param name="TraversedEdges">Рёбра, по которым прошла вода (используется для анимации в UI).</param>
public sealed record SimulationResult(
    IReadOnlySet<Receiver> ReachedReceivers,
    IReadOnlyList<Edge> TraversedEdges);
