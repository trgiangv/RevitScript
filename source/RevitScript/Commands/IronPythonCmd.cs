using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using RevitScript.Core;

namespace RevitScript.Commands;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class IronPythonCmd : ExternalCommand
{
    public override void Execute()
    {
        // load IronPython script
        var opts = new Dictionary<string, object> {{"Frames", true}, {"FullFrames", true}, {"LightweightScopes", true}};
        var engine = IronPython.Hosting.Python.CreateEngine(opts);

        engine.Runtime.LoadAssembly(typeof(TaskDialog).Assembly);
        engine.Runtime.LoadAssembly(typeof(Document).Assembly);
        
        var startupScript = "F:\\DIG_GiangVu\\workspace\\pyDCMvn\\pyDCMvn\\IronPython\\testRevitScript.py";
        {
            var executor = new ScriptExecutor(UiApplication);
            var result = executor.ExecuteScript(startupScript);
            if (result == Result.Failed) {
                TaskDialog.Show("Error Loading RevitScript", executor.Message);
            }
        }
    }
}