namespace AquaFlow.Core;

/// <summary>
/// Реализация <see cref="IPipeSimulator"/>: обход графа сети (BFS) от выбранного
/// входа по «открытым» согласно клапанам рёбрам.
/// </summary>
/// <remarks>
/// Топология сети (см. ТЗ, раздел 5.1) — ориентированный ацикличный граф:
/// 3 входа → 7 узлов (4 слоя) → 3 приёмника.
/// Диверт направляет воду ровно в одно из двух исходящих рёбер (по значению клапана).
/// Сплиттер при клапане = 1 пускает воду сразу в оба исходящих ребра.
/// </remarks>
public sealed class PipeSimulator : IPipeSimulator
{
    /// <summary>Узел сети, в который ведёт вход при valve = 0 (единственный вариант для входа).</summary>
    private static readonly IReadOnlyDictionary<Source, string> EntryJunction = new Dictionary<Source, string>
    {
        [Source.A] = "J1",
        [Source.B] = "J2",
        [Source.C] = "J3"
    };

    /// <summary>
    /// Маршрутизация каждого узла: (назначения при valve=0, назначения при valve=1).
    /// У диверта в каждом наборе ровно одно назначение, у сплиттера при valve=1 — два.
    /// </summary>
    private static readonly IReadOnlyDictionary<Junction, (string[] Valve0, string[] Valve1)> Routing =
        new Dictionary<Junction, (string[], string[])>
        {
            [Junction.J1] = (new[] { "J4" }, new[] { "J5" }),
            [Junction.J2] = (new[] { "J4" }, new[] { "J4", "J5" }),
            [Junction.J3] = (new[] { "J5" }, new[] { "J6" }),
            [Junction.J4] = (new[] { "J6" }, new[] { "J7" }),
            [Junction.J5] = (new[] { "J7" }, new[] { "J6", "J7" }),
            [Junction.J6] = (new[] { "X" }, new[] { "Y" }),
            [Junction.J7] = (new[] { "Y" }, new[] { "Z" })
        };

    public SimulationResult Run(SimConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var traversedEdges = new List<Edge>();
        var reachedReceivers = new HashSet<Receiver>();
        var expanded = new HashSet<string>();
        var queue = new Queue<string>();

        // Вход всегда ведёт ровно в один начальный узел.
        var sourceId = config.Source.ToString();
        var entryJunctionId = EntryJunction[config.Source];
        traversedEdges.Add(new Edge(sourceId, entryJunctionId));
        queue.Enqueue(entryJunctionId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            // Узел раскрываем (обрабатываем его исходящие рёбра) не более одного раза,
            // даже если в него пришло несколько входящих путей от разных узлов/сплиттеров.
            if (!expanded.Add(currentId))
            {
                continue;
            }

            var junction = Enum.Parse<Junction>(currentId);
            var valve = config.Valves[junction];
            var (valve0, valve1) = Routing[junction];
            var targets = valve == 0 ? valve0 : valve1;

            foreach (var targetId in targets)
            {
                traversedEdges.Add(new Edge(currentId, targetId));

                if (Enum.TryParse<Receiver>(targetId, out var receiver))
                {
                    reachedReceivers.Add(receiver);
                }
                else
                {
                    queue.Enqueue(targetId);
                }
            }
        }

        return new SimulationResult(reachedReceivers, traversedEdges);
    }
}
