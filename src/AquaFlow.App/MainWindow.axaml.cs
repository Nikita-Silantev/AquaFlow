using Avalonia.Controls;
using AquaFlow.Ml;

namespace AquaFlow.App;

public partial class MainWindow : Window
{
    /// <summary>Нужен загрузчику XAML/дизайнеру; зависимости в этом случае не передаются.</summary>
    public MainWindow() : this(null, null)
    {
    }

    public MainWindow(IWaterPredictor? predictor, IRunRepository? runRepository)
    {
        InitializeComponent();
        SimulationTab.Initialize(predictor, runRepository);
    }
}
