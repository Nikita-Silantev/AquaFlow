namespace AquaFlow.App.Tools;

/// <summary>
/// Находит корень репозитория (по файлу AquaFlow.sln) независимо от рабочей директории
/// процесса — нужно и для CLI-команд (--generate-dataset, --train-model), и для загрузки
/// модели при обычном старте GUI.
/// </summary>
internal static class RepoPaths
{
    public static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AquaFlow.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Не удалось найти корень репозитория (AquaFlow.sln).");
    }

    public static string ModelFilePath() => Path.Combine(FindRepoRoot(), "models", "water_mlp.bin");
}
