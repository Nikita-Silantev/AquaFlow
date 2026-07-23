using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AquaFlow.App.Tools;
using AquaFlow.Core;
using AquaFlow.Ml;

namespace AquaFlow.App.Views;

/// <summary>
/// Вкладка «Метрики» (M6, ТЗ раздел 8.3): метаданные модели, subset/per-label accuracy
/// и матрица ошибок на отложенном тесте, точность по прогонам в приложении, уверенность
/// для последней рассчитанной конфигурации.
/// </summary>
public partial class MetricsView : UserControl
{
    private AppServices? _services;

    public MetricsView()
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

        StatusText.Text = "Обновляю...";

        try
        {
            await LoadModelMetaAsync();
            await LoadTestMetricsAsync();
            await LoadRunsAccuracyAsync();
            LoadLastConfigConfidence();

            StatusText.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка обновления метрик: {ex.Message}";
        }
    }

    private async Task LoadModelMetaAsync()
    {
        var meta = await _services!.ModelMetaRepository.GetLatestAsync();
        ModelMetaText.Text = meta is null
            ? "Модель ещё не обучена (выполните: dotnet run --project src/AquaFlow.App -- --train-model)."
            : $"Обучена: {meta.TrainedAt.ToLocalTime():g}, эпох: {meta.Epochs}, " +
              $"subset accuracy на момент обучения: {meta.SubsetAccuracy:P1}, файл: {meta.FilePath}";
    }

    private async Task LoadTestMetricsAsync()
    {
        if (_services!.Predictor is null)
        {
            SubsetAccuracyText.Text = "Модель не загружена — метрики недоступны.";
            PerLabelAccuracyText.Text = string.Empty;
            ClearConfusionMatrix();
            return;
        }

        var samples = await _services.SampleRepository.GetAllAsync();
        if (samples.Count == 0)
        {
            SubsetAccuracyText.Text = "Датасет пуст (выполните --generate-dataset).";
            PerLabelAccuracyText.Text = string.Empty;
            ClearConfusionMatrix();
            return;
        }

        // То же самое train/test разбиение (фиксированный seed), что и при обучении в M4 —
        // тест остаётся отложенным, метрики честные.
        var split = TrainTestSplitter.Split(samples);
        var metrics = ModelEvaluator.Evaluate(_services.Predictor, split.Test);

        SubsetAccuracyText.Text =
            $"Subset accuracy (отложенный тест, {split.Test.Count} записей): {metrics.SubsetAccuracy:P1}";
        PerLabelAccuracyText.Text = "Per-label accuracy: " + string.Join(", ",
            metrics.PerLabelAccuracy.OrderBy(kv => kv.Key.ToString()).Select(kv => $"{kv.Key}={kv.Value:P1}"));

        FillConfusionMatrix(metrics);
    }

    private void FillConfusionMatrix(EvaluationMetrics metrics)
    {
        SetConfusionRow(metrics.ConfusionMatrices[Receiver.X], XTpText, XFpText, XFnText, XTnText);
        SetConfusionRow(metrics.ConfusionMatrices[Receiver.Y], YTpText, YFpText, YFnText, YTnText);
        SetConfusionRow(metrics.ConfusionMatrices[Receiver.Z], ZTpText, ZFpText, ZFnText, ZTnText);
    }

    private static void SetConfusionRow(LabelConfusion confusion, TextBlock tp, TextBlock fp, TextBlock fn, TextBlock tn)
    {
        tp.Text = confusion.TruePositive.ToString();
        fp.Text = confusion.FalsePositive.ToString();
        fn.Text = confusion.FalseNegative.ToString();
        tn.Text = confusion.TrueNegative.ToString();
    }

    private void ClearConfusionMatrix()
    {
        foreach (var text in new[]
                 {
                     XTpText, XFpText, XFnText, XTnText,
                     YTpText, YFpText, YFnText, YTnText,
                     ZTpText, ZFpText, ZFnText, ZTnText
                 })
        {
            text.Text = "—";
        }
    }

    private async Task LoadRunsAccuracyAsync()
    {
        var summary = await _services!.RunRepository.GetAccuracySummaryAsync();
        RunsAccuracyText.Text = summary.Total == 0
            ? "Пока нет сравнений предсказаний с реальностью в приложении."
            : $"{summary.Correct} из {summary.Total} прогонов совпало с предсказанием ({summary.Accuracy:P1}).";
    }

    private void LoadLastConfigConfidence()
    {
        var state = _services!.LastPredictionState;
        if (state.Prediction is null || state.Config is null)
        {
            LastConfigText.Text = "Ещё не было ни одного расчёта на вкладке «Симуляция».";
            SetConfidence(XConfidenceBar, XConfidenceValueText, 0);
            SetConfidence(YConfidenceBar, YConfidenceValueText, 0);
            SetConfidence(ZConfidenceBar, ZConfidenceValueText, 0);
            return;
        }

        var config = state.Config;
        var valvesText = string.Join(", ",
            config.Valves.OrderBy(kv => kv.Key.ToString()).Select(kv => $"{kv.Key}={kv.Value}"));
        LastConfigText.Text = $"Вход {config.Source}, клапаны: {valvesText}";

        var probabilities = state.Prediction.Probabilities;
        SetConfidence(XConfidenceBar, XConfidenceValueText, probabilities.GetValueOrDefault(Receiver.X));
        SetConfidence(YConfidenceBar, YConfidenceValueText, probabilities.GetValueOrDefault(Receiver.Y));
        SetConfidence(ZConfidenceBar, ZConfidenceValueText, probabilities.GetValueOrDefault(Receiver.Z));
    }

    private static void SetConfidence(ProgressBar bar, TextBlock valueText, float probability)
    {
        bar.Value = probability * 100;
        valueText.Text = probability.ToString("P0");
    }
}
