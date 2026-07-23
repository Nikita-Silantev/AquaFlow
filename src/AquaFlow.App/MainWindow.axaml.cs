using Avalonia.Controls;
using AquaFlow.App.Tools;

namespace AquaFlow.App;

public partial class MainWindow : Window
{
    /// <summary>Нужен загрузчику XAML/дизайнеру — без зависимостей вкладки останутся пустыми.</summary>
    public MainWindow() : this(null)
    {
    }

    public MainWindow(AppServices? services)
    {
        InitializeComponent();

        if (services is not null)
        {
            SimulationTab.Initialize(services);
            MetricsTab.Initialize(services);
            HistoryTab.Initialize(services);
        }
    }
}
