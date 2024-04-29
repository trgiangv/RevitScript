using RevitScriptTest.ViewModels;

namespace RevitScriptTest.Views;

public sealed partial class RevitScriptTestView
{
    public RevitScriptTestView(RevitScriptTestViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}