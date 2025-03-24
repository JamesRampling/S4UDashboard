using System.Collections.ObjectModel;
using System.Linq;

using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class MainModelView : ViewModelBase
{
    public RefCollection<FileTabViewModel> OpenFiles { get; }
    public ComputedValue<bool> AnyOpenFiles { get; }

    public MainModelView()
    {
        OpenFiles = new([]);

        AnyOpenFiles = ComputedValue<bool>.Builder()
            .WithDependency(OpenFiles)
            .Build((ObservableCollection<FileTabViewModel> c) => c.Any());

        OpenFiles.Value.Add(new());
    }

    public void MakeNew() => OpenFiles.Value.Add(new());
}
