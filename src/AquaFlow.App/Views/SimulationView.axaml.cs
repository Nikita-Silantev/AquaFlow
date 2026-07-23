using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using AquaFlow.Core;

namespace AquaFlow.App.Views;

/// <summary>
/// Вкладка «Симуляция»: канвас сети, выбор входа, переключение клапанов мышью
/// и кнопка «Реальный прогон» с анимацией потока (M2).
/// </summary>
public partial class SimulationView : UserControl
{
    private static readonly IBrush PipeIdleBrush = Brushes.Gainsboro;
    private static readonly IBrush PipeWetBrush = Brushes.DodgerBlue;
    private static readonly IBrush SourceIdleBrush = Brushes.LightGray;
    private static readonly IBrush SourceSelectedBrush = Brushes.DodgerBlue;
    private static readonly IBrush ValveClosedBrush = Brushes.LightGray;
    private static readonly IBrush ValveOpenBrush = Brushes.Orange;
    private static readonly IBrush ReceiverIdleBrush = Brushes.WhiteSmoke;
    private static readonly IBrush ReceiverReachedBrush = Brushes.DodgerBlue;

    // Работа с симулятором — только через интерфейс IPipeSimulator (см. ТЗ, раздел 10).
    private readonly IPipeSimulator _simulator = new PipeSimulator();

    private readonly Dictionary<string, Border> _nodeBorders = new();
    private readonly Dictionary<string, TextBlock> _nodeLabels = new();
    private readonly Dictionary<string, Line> _pipeLines = new();

    private readonly Dictionary<Junction, int> _valveState = new()
    {
        [Junction.J1] = 0,
        [Junction.J2] = 0,
        [Junction.J3] = 0,
        [Junction.J4] = 0,
        [Junction.J5] = 0,
        [Junction.J6] = 0,
        [Junction.J7] = 0
    };

    private Source _selectedSource = Source.A;
    private bool _isRunning;

    public SimulationView()
    {
        InitializeComponent();
        BuildNetwork();
        RedrawState();
    }

    /// <summary>Создаёт визуальные элементы канваса один раз при инициализации.</summary>
    private void BuildNetwork()
    {
        // Сначала трубы, чтобы узлы отрисовывались поверх линий.
        foreach (var (from, to) in NetworkLayout.AllPipes)
        {
            var fromNode = NetworkLayout.Nodes.First(n => n.Id == from);
            var toNode = NetworkLayout.Nodes.First(n => n.Id == to);

            var line = new Line
            {
                StartPoint = new Point(fromNode.X, fromNode.Y),
                EndPoint = new Point(toNode.X, toNode.Y),
                Stroke = PipeIdleBrush,
                StrokeThickness = 3
            };

            _pipeLines[$"{from}->{to}"] = line;
            NetworkCanvas.Children.Add(line);
        }

        foreach (var node in NetworkLayout.Nodes)
        {
            var label = new TextBlock
            {
                Text = node.Label,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center
            };

            var border = new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1.5),
                Background = Brushes.WhiteSmoke,
                Child = label
            };

            Canvas.SetLeft(border, node.X - 28);
            Canvas.SetTop(border, node.Y - 28);

            var nodeId = node.Id; // локальная копия для замыкания в обработчике
            var nodeKind = node.Kind;
            border.PointerPressed += (_, _) => OnNodeClicked(nodeId, nodeKind);

            _nodeBorders[node.Id] = border;
            _nodeLabels[node.Id] = label;
            NetworkCanvas.Children.Add(border);
        }
    }

    /// <summary>Клик по узлу: выбор входа или переключение клапана. Приёмники не кликабельны.</summary>
    private void OnNodeClicked(string nodeId, NetworkLayout.NodeKind kind)
    {
        if (_isRunning)
        {
            return;
        }

        switch (kind)
        {
            case NetworkLayout.NodeKind.Source:
                _selectedSource = Enum.Parse<Source>(nodeId);
                break;

            case NetworkLayout.NodeKind.Junction:
                var junction = Enum.Parse<Junction>(nodeId);
                _valveState[junction] = _valveState[junction] == 0 ? 1 : 0;
                break;

            case NetworkLayout.NodeKind.Receiver:
                return;
        }

        ResetRunVisuals();
        RedrawState();
    }

    /// <summary>Перерисовывает состояние узлов (выбранный вход, состояния клапанов).</summary>
    private void RedrawState()
    {
        foreach (var node in NetworkLayout.Nodes)
        {
            var border = _nodeBorders[node.Id];
            var label = _nodeLabels[node.Id];

            switch (node.Kind)
            {
                case NetworkLayout.NodeKind.Source:
                    var source = Enum.Parse<Source>(node.Id);
                    border.Background = source == _selectedSource ? SourceSelectedBrush : SourceIdleBrush;
                    label.Text = node.Label;
                    break;

                case NetworkLayout.NodeKind.Junction:
                    var junction = Enum.Parse<Junction>(node.Id);
                    var valve = _valveState[junction];
                    border.Background = valve == 1 ? ValveOpenBrush : ValveClosedBrush;
                    label.Text = $"{node.Label}\n{valve}";
                    break;

                case NetworkLayout.NodeKind.Receiver:
                    label.Text = node.Label;
                    break;
            }
        }
    }

    /// <summary>Сбрасывает результат предыдущего прогона (трубы и приёмники — в нейтральный цвет).</summary>
    private void ResetRunVisuals()
    {
        foreach (var line in _pipeLines.Values)
        {
            line.Stroke = PipeIdleBrush;
            line.StrokeThickness = 3;
        }

        foreach (var node in NetworkLayout.Nodes.Where(n => n.Kind == NetworkLayout.NodeKind.Receiver))
        {
            _nodeBorders[node.Id].Background = ReceiverIdleBrush;
        }

        ResultText.Text = "Выберите вход и клапаны, затем нажмите «Реальный прогон».";
    }

    private async void OnRunRealClick(object? sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        RunRealButton.IsEnabled = false;
        ResetRunVisuals();

        var config = SimConfig.Create(_selectedSource, _valveState);
        var result = _simulator.Run(config);

        // Анимация: последовательно подсвечиваем рёбра, по которым прошла вода.
        foreach (var edge in result.TraversedEdges)
        {
            if (_pipeLines.TryGetValue($"{edge.From}->{edge.To}", out var line))
            {
                line.Stroke = PipeWetBrush;
                line.StrokeThickness = 5;
            }

            await Task.Delay(220);
        }

        // Заливка синим приёмников, до которых дошла вода.
        foreach (var receiver in result.ReachedReceivers)
        {
            _nodeBorders[receiver.ToString()].Background = ReceiverReachedBrush;
        }

        ResultText.Text = result.ReachedReceivers.Count > 0
            ? $"Вода дошла до: {string.Join(", ", result.ReachedReceivers.Select(r => r.ToString()).OrderBy(s => s))}"
            : "Вода никуда не дошла.";

        RunRealButton.IsEnabled = true;
        _isRunning = false;

        await ShowResultWindowAsync(config, result);
    }

    /// <summary>Показывает окно итога прогона (вход, клапаны, достигнутые приёмники).</summary>
    private async Task ShowResultWindowAsync(SimConfig config, SimulationResult result)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var window = new RunResultWindow(config, result);

        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }
    }
}
