using Xunit;

namespace AquaFlow.Core.Tests;

/// <summary>
/// Тесты детерминированного симулятора сети. Ожидаемые результаты для каждого кейса
/// посчитаны вручную по топологии из ТЗ (раздел 5.1):
///
/// A→J1, B→J2, C→J3
/// J1 (диверт):    0→J4,        1→J5
/// J2 (сплиттер):  0→J4,        1→J4 и J5
/// J3 (диверт):    0→J5,        1→J6
/// J4 (диверт):    0→J6,        1→J7
/// J5 (сплиттер):  0→J7,        1→J6 и J7
/// J6 (диверт):    0→X,         1→Y
/// J7 (диверт):    0→Y,         1→Z
/// </summary>
public class PipeSimulatorTests
{
    private readonly PipeSimulator _simulator = new();

    /// <summary>Строит словарь клапанов из семи значений в порядке J1..J7.</summary>
    private static IReadOnlyDictionary<Junction, int> Valves(
        int j1, int j2, int j3, int j4, int j5, int j6, int j7) =>
        new Dictionary<Junction, int>
        {
            [Junction.J1] = j1,
            [Junction.J2] = j2,
            [Junction.J3] = j3,
            [Junction.J4] = j4,
            [Junction.J5] = j5,
            [Junction.J6] = j6,
            [Junction.J7] = j7
        };

    [Fact(DisplayName = "A, все клапаны 0 → A-J1-J4-J6-X, единственный приёмник X")]
    public void Source_A_AllZero_ReachesX()
    {
        var config = SimConfig.Create(Source.A, Valves(0, 0, 0, 0, 0, 0, 0));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.X }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "B, все клапаны 0 → B-J2-J4-J6-X, единственный приёмник X")]
    public void Source_B_AllZero_ReachesX()
    {
        var config = SimConfig.Create(Source.B, Valves(0, 0, 0, 0, 0, 0, 0));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.X }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "C, все клапаны 0 → C-J3-J5-J7-Y, «нижний» маршрут отличается от A/B")]
    public void Source_C_AllZero_ReachesY()
    {
        var config = SimConfig.Create(Source.C, Valves(0, 0, 0, 0, 0, 0, 0));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.Y }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "A, все клапаны 1 → сплиттер J5 даёт два приёмника Y и Z")]
    public void Source_A_AllOne_ReachesYAndZ()
    {
        // A-J1(1)-J5; J5 сплиттер valve=1 → J6 и J7; J6(1)→Y; J7(1)→Z.
        var config = SimConfig.Create(Source.A, Valves(1, 1, 1, 1, 1, 1, 1));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.Y, Receiver.Z }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "C, все клапаны 1 → единственный приёмник Y (крайняя конфигурация)")]
    public void Source_C_AllOne_ReachesY()
    {
        // C-J3(1)-J6(1)→Y.
        var config = SimConfig.Create(Source.C, Valves(1, 1, 1, 1, 1, 1, 1));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.Y }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "B со сплиттером J2=1 → два приёмника X и Y")]
    public void Source_B_SplitterJ2_ReachesXAndY()
    {
        // B-J2(1) сплиттер → J4 и J5; J4(0)→J6(0)→X; J5(0)→J7(0)→Y.
        var config = SimConfig.Create(Source.B, Valves(0, 1, 0, 0, 0, 0, 0));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.X, Receiver.Y }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "C со сплиттером J5=1 глубже в графе → два приёмника X и Z")]
    public void Source_C_SplitterJ5_ReachesXAndZ()
    {
        // C-J3(0)-J5(1) сплиттер → J6 и J7; J6(0)→X; J7(1)→Z.
        var config = SimConfig.Create(Source.C, Valves(0, 0, 0, 0, 1, 0, 1));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.X, Receiver.Z }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "A, одиночный маршрут глубиной 3 звена → приёмник Y")]
    public void Source_A_SinglePathDepthThree_ReachesY()
    {
        // A-J1(0)-J4(1)-J7(0)→Y.
        var config = SimConfig.Create(Source.A, Valves(0, 0, 0, 1, 0, 0, 0));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.Y }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "A, одиночный маршрут глубиной 4 звена через J5 → приёмник Z")]
    public void Source_A_SinglePathDepthFour_ReachesZ()
    {
        // A-J1(1)-J5(0)-J7(1)→Z. J5 сплиттер, но при valve=0 у него единственная цель J7.
        var config = SimConfig.Create(Source.A, Valves(1, 0, 0, 0, 0, 0, 1));

        var result = _simulator.Run(config);

        Assert.Equal(new HashSet<Receiver> { Receiver.Z }, result.ReachedReceivers);
    }

    [Fact(DisplayName = "Симулятор детерминирован: одинаковый config → одинаковый результат")]
    public void Run_SameConfig_ReturnsSameResult()
    {
        var config = SimConfig.Create(Source.B, Valves(0, 1, 0, 0, 0, 0, 0));

        var first = _simulator.Run(config);
        var second = _simulator.Run(config);

        Assert.Equal(first.ReachedReceivers, second.ReachedReceivers);
        Assert.Equal(first.TraversedEdges, second.TraversedEdges);
    }

    [Fact(DisplayName = "TraversedEdges содержит точную последовательность рёбер маршрута")]
    public void Run_AllZeroFromA_TraversedEdgesMatchExpectedPath()
    {
        var config = SimConfig.Create(Source.A, Valves(0, 0, 0, 0, 0, 0, 0));

        var result = _simulator.Run(config);

        var expected = new[]
        {
            new Edge("A", "J1"),
            new Edge("J1", "J4"),
            new Edge("J4", "J6"),
            new Edge("J6", "X")
        };
        Assert.Equal(expected, result.TraversedEdges);
    }

    [Fact(DisplayName = "SimConfig.Create бросает исключение, если клапан не задан или задан некорректно")]
    public void Create_InvalidValves_Throws()
    {
        var incompleteValves = new Dictionary<Junction, int>
        {
            [Junction.J1] = 0,
            [Junction.J2] = 0,
            [Junction.J3] = 0,
            [Junction.J4] = 0,
            [Junction.J5] = 0,
            [Junction.J6] = 0
            // J7 не задан
        };

        Assert.Throws<ArgumentException>(() => SimConfig.Create(Source.A, incompleteValves));
    }
}
