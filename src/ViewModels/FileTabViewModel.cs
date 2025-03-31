using S4UDashboard.Model;
using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class FileTabViewModel(DatasetModel dataset) : ViewModelBase
{
    public ReactiveCell<DatasetModel> Dataset { get; } = new(dataset);

    public string Header => Dataset.Value.AnnotatedData.AnnotatedName ?? Dataset.Value.FileName;
    public ReactiveCell<string> TextField { get; } = new("Initial");

    public void LowercasifyCommand()
    {
        TextField.Value = TextField.Value.ToLower();
    }
}
