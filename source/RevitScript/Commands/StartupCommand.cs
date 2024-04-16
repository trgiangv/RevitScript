using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace RevitScript.Commands;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        // if (WindowController.Focus<RevitScriptView>()) return;
        //
        // var view = Host.GetService<RevitScriptView>();
        // WindowController.Show(view, UiApplication.MainWindowHandle);
        
        // load IronPython script
        var opts = new Dictionary<string, object>() {{"Frames", true}, {"FullFrames", true}, {"LightweightScopes", true}};
        var ironPython = IronPython.Hosting.Python.CreateEngine(opts);
        
        ironPython.Runtime.LoadAssembly(typeof(TaskDialog).Assembly);
        ironPython.Runtime.LoadAssembly(typeof(Document).Assembly);
        
        var startupScript = "F:\\DIG_GiangVu\\workspace\\pyDCMvn\\pyDCMvn\\IronPython\\testRevitScript.py";
        {
            var executor = new ScriptExecutor(UiApplication);
            var result = executor.ExecuteScript(startupScript);
            if (result == Result.Failed) {
                TaskDialog.Show("Error Loading pyRevit", executor.Message);
            }
        }
    }
}