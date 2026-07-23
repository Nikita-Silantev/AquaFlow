using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AquaFlow.App.Tools;
using AquaFlow.Ml;

namespace AquaFlow.App.Views;

/// <summary>
/// Вкладка «История» (M6, ТЗ раздел 8.4): список прошлых прогонов из таблицы `runs` —
/// время, вход, клапаны, предсказание, факт, совпадение. Простая прокручиваемая таблица
/// без DataGrid (не добавляем зависимость сверх стека, указанного в ТЗ).
/// </summary>
public partial class HistoryView : UserControl
{
    private AppServices? _services;

    public HistoryView()
    {
        InitializeComponent();
    }

    public void Initialize(AppServices services)
    {
        _services = services;
        _ = RefreshAsync();
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_services is null)
        {
            return;
        }

        StatusText.Text = "Загружаю...";
        HistoryRows.Children.Clear();

        try
        {
            var history = await _services.RunRepository.GetHistoryAsync();

            if (history.Count == 0)
            {
                StatusText.Text = "Прогонов ещё не было — сходите на вкладку «Симуляция».";
                return;
            }

            foreach (var entry in history)
            {
                HistoryRows.Children.Add(BuildRow(entry));
            }

            StatusText.Text = $"Показано записей: {history.Count}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка загрузки истории: {ex.Message}";
        }
    }

    private static Grid BuildRow(RunHistoryEntry entry)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("160,60,240,140,140,80"),
            Margin = new Thickness(0, 2)
        };

        var valvesText = string.Join(",",
            entry.Valves.OrderBy(kv => kv.Key.ToString()).Select(kv => $"{kv.Key}={kv.Value}"));

        var predictedText = entry.PredictedReceivers is null
            ? "—"
            : entry.PredictedReceivers.Count > 0
                ? string.Join(",", entry.PredictedReceivers.Select(r => r.ToString()).OrderBy(s => s))
                : "ничего";

        var actualText = entry.ActualReceivers.Count > 0
            ? string.Join(",", entry.ActualReceivers.Select(r => r.ToString()).OrderBy(s => s))
            : "ничего";

        var (correctText, correctBrush) = entry.WasCorrect switch
        {
            true => ("✓", (IBrush)Brushes.SeaGreen),
            false => ("✗", (IBrush)Brushes.OrangeRed),
            null => ("—", (IBrush)Brushes.Gray)
        };

        AddCell(grid, entry.CreatedAt.ToLocalTime().ToString("g"), 0);
        AddCell(grid, entry.Source.ToString(), 1);
        AddCell(grid, valvesText, 2);
        AddCell(grid, predictedText, 3);
        AddCell(grid, actualText, 4);
        AddCell(grid, correctText, 5, correctBrush);

        return grid;
    }

    private static void AddCell(Grid grid, string text, int column, IBrush? foreground = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.NoWrap
        };

        if (foreground is not null)
        {
            textBlock.Foreground = foreground;
        }

        Grid.SetColumn(textBlock, column);
        grid.Children.Add(textBlock);
    }
}
