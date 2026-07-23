namespace AquaFlow.Ml;

/// <summary>Результат разбиения датасета на обучающую и отложенную (тестовую) выборки.</summary>
public sealed record DatasetSplit(IReadOnlyList<Sample> Train, IReadOnlyList<Sample> Test);
