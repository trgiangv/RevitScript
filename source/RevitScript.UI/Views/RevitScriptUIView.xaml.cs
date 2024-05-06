using RevitScript.UI.ViewModels;

namespace RevitScript.UI.Views;

public sealed partial class RevitScriptUIView
{
    public RevitScriptUIView(RevitScriptUIViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}