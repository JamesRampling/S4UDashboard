using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class FileTabViewModel : ViewModelBase
{
    public static string Header => "Foo";
    public RefValue<string> TextField { get; } = new("Initial");

    public void LowercasifyCommand()
    {
        TextField.Value = TextField.Value.ToLower();
    }
}
