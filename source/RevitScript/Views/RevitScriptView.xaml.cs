using RevitScript.ViewModels;

namespace RevitScript.Views;

public sealed partial class RevitScriptView
{
    public RevitScriptView(RevitScriptViewModel scriptViewModel)
    {
        DataContext = scriptViewModel;
        InitializeComponent();
    }
}