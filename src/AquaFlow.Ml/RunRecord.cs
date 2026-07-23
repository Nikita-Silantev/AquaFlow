using AquaFlow.Core;

namespace AquaFlow.Ml;

/// <summary>
/// Одна запись истории прогонов (таблица `runs`, ТЗ раздел 7). В M5 пишутся только
/// реальные прогоны (mode = "real"); если перед ними было сделано предсказание для той же
/// конфигурации — предсказанные поля заполнены и известен <see cref="WasCorrect"/>.
/// </summary>
public sealed record RunRecord(
    Source Source,
    IReadOnlyDictionary<Junction, int> Valves,
    string Mode,
    IReadOnlySet<Receiver>? PredictedReceivers,
    IReadOnlyDictionary<Receiver, float>? PredictedProbabilities,
    IReadOnlySet<Receiver> ActualReceivers,
    bool? WasCorrect);

/// <summary>Агрегат для живого счётчика точности («модель угадала N из M прогонов»).</summary>
public sealed record RunAccuracySummary(int Total, int Correct)
{
    public double Accuracy => Total == 0 ? 0.0 : (double)Correct / Total;
}

/// <summary>Одна строка вкладки «История» (M6, ТЗ раздел 8.4).</summary>
public sealed record RunHistoryEntry(
    long Id,
    DateTimeOffset CreatedAt,
    Source Source,
    IReadOnlyDictionary<Junction, int> Valves,
    IReadOnlySet<Receiver>? PredictedReceivers,
    IReadOnlySet<Receiver> ActualReceivers,
    bool? WasCorrect);
