using Avalonia.Controls;

using S4UDashboard.Model;

using Tabalonia.Controls;

namespace S4UDashboard.Views;

/// <summary>The class that holds the main view.</summary>
public partial class MainView : UserControl
{
    /// <summary>Initialises the main view.</summary>
    public MainView()
    {
        InitializeComponent();
        AddHandler(DragTabItem.DragDelta, (o, e) => sortbox.SelectedItem = SortMode.Unsorted, handledEventsToo: true);
    }
}
