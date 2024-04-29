using RevitScriptUI.ViewModels;

namespace RevitScriptUI.Views;

public sealed partial class RevitScriptUIView
{
    public RevitScriptUIView(RevitScriptUIViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}