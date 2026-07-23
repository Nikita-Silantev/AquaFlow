using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using AquaFlow.App.Tools;
using AquaFlow.Core;
using AquaFlow.Ml;

namespace AquaFlow.App.Views;

/// <summary>
/// Вкладка «Симуляция»: канвас сети, выбор входа, переключение клапанов мышью,
/// кнопки «Расчёт» (нейросеть, M5) и «Реальный прогон» (M2) с анимацией потока,
/// сравнением предсказания с реальностью и живым счётчиком точности (M5).
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
    private static readonly IBrush BorderIdleBrush = Brushes.Black;
    private static readonly IBrush BorderPredictedBrush = Brushes.DodgerBlue;

    // Работа с симулятором и моделью — только через интерфейсы (см. ТЗ, раздел 10).
    private readonly IPipeSimulator _simulator = new PipeSimulator();
    private IWaterPredictor? _predictor;
    private IRunRepository? _runRepository;
    private LastPredictionState? _lastPredictionState;

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

    private Prediction? _lastPrediction;
    private SimConfig? _lastPredictionConfig;
    private int _totalCompared;
    private int _correctCompared;

    public SimulationView()
    {
        InitializeComponent();
        BuildNetwork();
        RedrawState();
    }

    /// <summary>Передаёт вкладке зависимости, собранные при старте приложения (M5/M6).</summary>
    public void Initialize(AppServices services)
    {
        _predictor = services.Predictor;
        _runRepository = services.RunRepository;
        _lastPredictionState = services.LastPredictionState;

        PredictButton.IsEnabled = _predictor is not null;
        if (_predictor is null)
        {
            PredictionText.Text =
                "Модель не обучена. Выполните: dotnet run --project src/AquaFlow.App -- --train-model";
        }

        _ = LoadAccuracySummaryAsync();
    }

    private async Task LoadAccuracySummaryAsync()
    {
        if (_runRepository is null)
        {
            return;
        }

        try
        {
            var summary = await _runRepository.GetAccuracySummaryAsync();
            _totalCompared = summary.Total;
            _correctCompared = summary.Correct;
            UpdateAccuracyText();
        }
        catch (Exception ex)
        {
            AccuracyText.Text = $"Не удалось загрузить счётчик точности из БД: {ex.Message}";
        }
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
                TextAlignment = TextAlignment.Center,
                // Явно чёрный: иначе текст наследует цвет из тёмной темы приложения
                // и становится нечитаемым на светлом фоне узла.
                Foreground = Brushes.Black
            };

            var border = new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                BorderBrush = BorderIdleBrush,
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

        // Конфигурация изменилась — и результат реального прогона, и предсказание устарели.
        ResetRunVisuals();
        ResetPredictionVisuals();
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

    /// <summary>Сбрасывает результат предыдущего реального прогона (трубы и заливка приёмников).</summary>
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

    /// <summary>Сбрасывает предсказание нейросети (синюю рамку приёмников) и текст вероятностей.</summary>
    private void ResetPredictionVisuals()
    {
        foreach (var node in NetworkLayout.Nodes.Where(n => n.Kind == NetworkLayout.NodeKind.Receiver))
        {
            var border = _nodeBorders[node.Id];
            border.BorderBrush = BorderIdleBrush;
            border.BorderThickness = new Thickness(1.5);
        }

        _lastPrediction = null;
        _lastPredictionConfig = null;

        PredictionText.Text = _predictor is null
            ? "Модель не обучена. Выполните: dotnet run --project src/AquaFlow.App -- --train-model"
            : "Нажмите «Расчёт», чтобы узнать предсказание нейросети.";
    }

    /// <summary>Кнопка «Расчёт»: вызывает IWaterPredictor.Predict и подсвечивает предсказанные приёмники рамкой.</summary>
    private void OnPredictClick(object? sender, RoutedEventArgs e)
    {
        if (_predictor is null || _isRunning)
        {
            return;
        }

        ResetPredictionVisuals();

        var config = SimConfig.Create(_selectedSource, _valveState);
        var prediction = _predictor.Predict(config);

        _lastPrediction = prediction;
        _lastPredictionConfig = config;
        _lastPredictionState?.Update(config, prediction);

        foreach (var receiver in prediction.PredictedReceivers)
        {
            var border = _nodeBorders[receiver.ToString()];
            border.BorderBrush = BorderPredictedBrush;
            border.BorderThickness = new Thickness(4);
        }

        var probabilitiesText = string.Join(", ", prediction.Probabilities
            .OrderBy(kv => kv.Key.ToString())
            .Select(kv => $"{kv.Key}={kv.Value:P0}"));

        var predictedText = prediction.PredictedReceivers.Count > 0
            ? string.Join(", ", prediction.PredictedReceivers.Select(r => r.ToString()).OrderBy(s => s))
            : "ничего";

        PredictionText.Text = $"Нейросеть предсказывает: {predictedText} ({probabilitiesText})";
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

        var reachedText = result.ReachedReceivers.Count > 0
            ? $"Вода дошла до: {string.Join(", ", result.ReachedReceivers.Select(r => r.ToString()).OrderBy(s => s))}"
            : "Вода никуда не дошла.";

        // Главный вау-момент (ТЗ, раздел 8.2): если перед этим было сделано предсказание
        // для той же конфигурации — сравниваем его с реальным результатом.
        bool? wasCorrect = null;
        if (_lastPrediction is not null && _lastPredictionConfig is not null && ConfigsEqual(_lastPredictionConfig, config))
        {
            wasCorrect = _lastPrediction.PredictedReceivers.SetEquals(result.ReachedReceivers);
            _totalCompared++;
            if (wasCorrect.Value)
            {
                _correctCompared++;
            }

            UpdateAccuracyText();
            reachedText += wasCorrect.Value ? "  Нейросеть: совпало ✓" : "  Нейросеть: ошибка ✗";
        }

        ResultText.Text = reachedText;

        await PersistRunAsync(config, result, wasCorrect);

        RunRealButton.IsEnabled = true;
        _isRunning = false;

        await ShowResultWindowAsync(config, result, wasCorrect);
    }

    private async Task PersistRunAsync(SimConfig config, SimulationResult result, bool? wasCorrect)
    {
        if (_runRepository is null)
        {
            return;
        }

        var hasPrediction = wasCorrect is not null && _lastPrediction is not null;

        var run = new RunRecord(
            config.Source,
            config.Valves,
            "real",
            hasPrediction ? _lastPrediction!.PredictedReceivers : null,
            hasPrediction ? _lastPrediction!.Probabilities : null,
            result.ReachedReceivers,
            wasCorrect);

        try
        {
            await _runRepository.InsertAsync(run);
        }
        catch (Exception ex)
        {
            ResultText.Text += $"  (не удалось записать прогон в БД: {ex.Message})";
        }
    }

    private void UpdateAccuracyText()
    {
        AccuracyText.Text = _totalCompared == 0
            ? "Пока нет сравнений предсказаний с реальностью."
            : $"Модель угадала {_correctCompared} из {_totalCompared} прогонов " +
              $"({(double)_correctCompared / _totalCompared:P1}).";
    }

    private static bool ConfigsEqual(SimConfig a, SimConfig b)
    {
        if (a.Source != b.Source || a.Valves.Count != b.Valves.Count)
        {
            return false;
        }

        foreach (var (junction, value) in a.Valves)
        {
            if (!b.Valves.TryGetValue(junction, out var otherValue) || otherValue != value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Показывает окно итога прогона (вход, клапаны, достигнутые приёмники, сравнение с предсказанием).</summary>
    private async Task ShowResultWindowAsync(SimConfig config, SimulationResult result, bool? wasCorrect)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var window = new RunResultWindow(config, result, _lastPrediction, wasCorrect);

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
