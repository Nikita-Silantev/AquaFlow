namespace AquaFlow.App.Views;

/// <summary>
/// Статическая раскладка сети для отрисовки на канвасе: координаты узлов и полный
/// набор труб (все возможные рёбра из ТЗ, раздел 5.1), независимо от состояния клапанов.
/// Это чисто визуальные данные — доменная маршрутизация остаётся только в AquaFlow.Core.
/// </summary>
public static class NetworkLayout
{
    public enum NodeKind
    {
        Source,
        Junction,
        Receiver
    }

    public sealed record NodeVisual(string Id, string Label, double X, double Y, NodeKind Kind);

    public static readonly IReadOnlyList<NodeVisual> Nodes = new List<NodeVisual>
    {
        new("A", "A", 60, 90, NodeKind.Source),
        new("B", "B", 60, 290, NodeKind.Source),
        new("C", "C", 60, 490, NodeKind.Source),

        new("J1", "J1", 260, 90, NodeKind.Junction),
        new("J2", "J2", 260, 290, NodeKind.Junction),
        new("J3", "J3", 260, 490, NodeKind.Junction),

        new("J4", "J4", 460, 190, NodeKind.Junction),
        new("J5", "J5", 460, 390, NodeKind.Junction),

        new("J6", "J6", 660, 190, NodeKind.Junction),
        new("J7", "J7", 660, 390, NodeKind.Junction),

        new("X", "X", 860, 90, NodeKind.Receiver),
        new("Y", "Y", 860, 290, NodeKind.Receiver),
        new("Z", "Z", 860, 490, NodeKind.Receiver)
    };

    /// <summary>Полный набор труб сети (для отрисовки всех рёбер, до применения клапанов).</summary>
    public static readonly IReadOnlyList<(string From, string To)> AllPipes = new List<(string, string)>
    {
        ("A", "J1"), ("B", "J2"), ("C", "J3"),
        ("J1", "J4"), ("J1", "J5"),
        ("J2", "J4"), ("J2", "J5"),
        ("J3", "J5"), ("J3", "J6"),
        ("J4", "J6"), ("J4", "J7"),
        ("J5", "J6"), ("J5", "J7"),
        ("J6", "X"), ("J6", "Y"),
        ("J7", "Y"), ("J7", "Z")
    };
}
