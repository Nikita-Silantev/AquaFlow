using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AquaFlow.Core;

namespace AquaFlow.App.Views;

/// <summary>
/// Окно итога прогона: показывает вход, состояния клапанов и достигнутые приёмники.
/// </summary>
public partial class RunResultWindow : Window
{
    /// <summary>Конструктор без параметров нужен загрузчику XAML.</summary>
    public RunResultWindow()
    {
        InitializeComponent();
    }

    public RunResultWindow(SimConfig config, SimulationResult result) : this()
    {
        SourceText.Text = $"Вход: {config.Source}";

        ValvesText.Text = "Клапаны: " + string.Join(", ",
            config.Valves
                .OrderBy(kv => kv.Key.ToString())
                .Select(kv => $"{kv.Key}={kv.Value}"));

        ReceiversText.Text = result.ReachedReceivers.Count > 0
            ? $"Вода дошла до: {string.Join(", ", result.ReachedReceivers.Select(r => r.ToString()).OrderBy(s => s))}"
            : "Вода никуда не дошла.";
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
