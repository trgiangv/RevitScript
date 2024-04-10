using pyRevit.ViewModels;

namespace pyRevit.Views;

public sealed partial class pyRevitView
{
    public pyRevitView(pyRevitViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}