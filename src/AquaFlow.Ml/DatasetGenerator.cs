using AquaFlow.Core;

namespace AquaFlow.Ml;

/// <summary>
/// Генерирует полный датасет, перебирая все конфигурации и вызывая
/// <see cref="IPipeSimulator"/> — единственный источник истины для меток (ТЗ, раздел 6.3).
/// </summary>
public sealed class DatasetGenerator : IDatasetGenerator
{
    private readonly IPipeSimulator _simulator;

    public DatasetGenerator(IPipeSimulator simulator)
    {
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
    }

    public IReadOnlyList<Sample> GenerateFullDataset()
    {
        var junctions = Enum.GetValues<Junction>();
        var junctionCount = junctions.Length; // 7
        var configsPerSource = 1 << junctionCount; // 2^7 = 128

        var samples = new List<Sample>(3 * configsPerSource);

        foreach (var source in Enum.GetValues<Source>())
        {
            for (var mask = 0; mask < configsPerSource; mask++)
            {
                var valves = MaskToValves(mask, junctions);
                var config = SimConfig.Create(source, valves);
                var result = _simulator.Run(config);

                samples.Add(new Sample(source, valves, result.ReachedReceivers));
            }
        }

        return samples;
    }

    /// <summary>Разворачивает битовую маску в состояния клапанов J1..J7 (бит i → Junction[i]).</summary>
    private static IReadOnlyDictionary<Junction, int> MaskToValves(int mask, Junction[] junctions)
    {
        var valves = new Dictionary<Junction, int>(junctions.Length);
        for (var i = 0; i < junctions.Length; i++)
        {
            valves[junctions[i]] = (mask >> i) & 1;
        }

        return valves;
    }
}
