using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using RevitScriptTest.ViewModels;
using RevitScriptTest.Views;
using RevitScriptTest.Utils;

namespace RevitScriptTest.Commands;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<RevitScriptTestView>()) return;

        var viewModel = new RevitScriptTestViewModel();
        var view = new RevitScriptTestView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}