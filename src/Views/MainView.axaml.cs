using Avalonia.Controls;

using S4UDashboard.Model;

using Tabalonia.Controls;

namespace S4UDashboard.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        AddHandler(DragTabItem.DragDelta, (o, e) => sortbox.SelectedItem = SortMode.Unsorted, handledEventsToo: true);
    }
}
