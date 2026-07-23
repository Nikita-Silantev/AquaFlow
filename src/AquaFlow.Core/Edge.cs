namespace AquaFlow.Core;

/// <summary>
/// Ребро сети — труба между двумя узлами графа.
/// Узлы адресуются строковым идентификатором: "A"/"B"/"C" (входы),
/// "J1".."J7" (узлы), "X"/"Y"/"Z" (приёмники).
/// </summary>
/// <param name="From">Идентификатор узла-источника ребра.</param>
/// <param name="To">Идентификатор узла-назначения ребра.</param>
public sealed record Edge(string From, string To);
