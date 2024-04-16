using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;

namespace RevitScript;

/// <summary>
///     Application entry point
/// </summary>
[UsedImplicitly]
public class Application : IExternalApplication
{
    private static string LoaderPath => Path.GetDirectoryName(typeof(Application).Assembly.Location);
    public Result OnStartup(UIControlledApplication application)
    {
        Host.Start();
        try {
            foreach (var engineDll in Directory.GetFiles(LoaderPath, "*.dll"))
                Assembly.LoadFrom(engineDll);
    
            return ExecuteStartupScript(application);
        }
        catch (Exception ex) {
            TaskDialog.Show("Error Loading Startup Script", ex.ToString());
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }


    private static Result ExecuteStartupScript(UIControlledApplication uiControlledApplication) {
        // we need a UIApplication object to assign as `__revit__` in python...
        var fieldName = "m_uiapplication";
        var fi = uiControlledApplication.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        if (fi == null) return Result.Failed;
        var uiApplication = (UIApplication)fi.GetValue(uiControlledApplication);
        
        // execute StartupScript
        Result result = Result.Succeeded;
        var startupScript = GetStartupScriptPath();
        if (startupScript != null) {
            var executor = new ScriptExecutor(uiApplication);
            result = executor.ExecuteScript(startupScript);
            if (result == Result.Failed) {
                TaskDialog.Show("Error Loading pyRevit", executor.Message);
            }
        }

        return result;
    }

    private static string GetStartupScriptPath() {
        var loaderDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var dllDir = Path.GetDirectoryName(loaderDir);
        return dllDir != null ? Path.Combine(dllDir, $"{Assembly.GetExecutingAssembly().GetName().Name}.py") : null;
    }
    
}