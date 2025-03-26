using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class MainModelView : ViewModelBase
{
    public ReactiveList<FileTabViewModel> OpenFiles { get; } = [new()];
    public ComputedCell<bool> AnyOpenFiles { get; }

    public MainModelView()
    {
        AnyOpenFiles = new(() => OpenFiles.Count != 0);
    }

    public void MakeNew() => OpenFiles.Add(new());
}
