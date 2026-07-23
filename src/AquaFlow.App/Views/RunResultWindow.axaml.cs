using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AquaFlow.Core;
using AquaFlow.Ml;

namespace AquaFlow.App.Views;

/// <summary>
/// Окно итога прогона: вход, состояния клапанов, достигнутые приёмники и (M5) —
/// сравнение с предсказанием нейросети, если оно было сделано для этой же конфигурации.
/// </summary>
public partial class RunResultWindow : Window
{
    /// <summary>Конструктор без параметров нужен загрузчику XAML.</summary>
    public RunResultWindow()
    {
        InitializeComponent();
    }

    public RunResultWindow(SimConfig config, SimulationResult result, Prediction? prediction, bool? wasCorrect)
        : this()
    {
        SourceText.Text = $"Вход: {config.Source}";

        ValvesText.Text = "Клапаны: " + string.Join(", ",
            config.Valves
                .OrderBy(kv => kv.Key.ToString())
                .Select(kv => $"{kv.Key}={kv.Value}"));

        ReceiversText.Text = result.ReachedReceivers.Count > 0
            ? $"Вода дошла до: {string.Join(", ", result.ReachedReceivers.Select(r => r.ToString()).OrderBy(s => s))}"
            : "Вода никуда не дошла.";

        if (prediction is not null && wasCorrect is not null)
        {
            var predictedText = prediction.PredictedReceivers.Count > 0
                ? string.Join(", ", prediction.PredictedReceivers.Select(r => r.ToString()).OrderBy(s => s))
                : "ничего";

            ComparisonText.Text = wasCorrect.Value
                ? $"Нейросеть предсказала: {predictedText} — совпало ✓"
                : $"Нейросеть предсказала: {predictedText} — ошибка ✗";
            ComparisonText.Foreground = wasCorrect.Value ? Brushes.SeaGreen : Brushes.OrangeRed;
        }
        else
        {
            ComparisonText.Text = string.Empty;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
