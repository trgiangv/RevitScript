using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using pyRevit.Views;
using pyRevit.Utils;

namespace pyRevit.Commands;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<pyRevitView>()) return;

        var view = Host.GetService<pyRevitView>();
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}