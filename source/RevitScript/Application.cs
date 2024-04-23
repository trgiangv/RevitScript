using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;
using RevitScript.Commands;

namespace RevitScript;

/// <summary>
///     Application entry point
/// </summary>
[UsedImplicitly]
public class Application : IExternalApplication
{
    private static string LoaderPath => Path.GetDirectoryName(typeof(Application).Assembly.Location);
    private UIControlledApplication _uiControlledApplication;

    public Result OnStartup(UIControlledApplication application)
    {
        // create a new button
        _uiControlledApplication = application;
        CreateRibbon();

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

    private void CreateRibbon()
    {
        var panel = _uiControlledApplication.CreatePanel("Commands", "RevitScript");

        panel.AddPushButton<IronPythonCmd>("IronPython")
            .SetLargeImage("/RevitScript;component/Resources/Icons/RibbonIcon32.png");

        panel.AddPushButton<CPythonCmd>("CPython")
            .SetLargeImage("/RevitScript;component/Resources/Icons/RibbonIcon32.png");
        
        panel.AddPushButton<HotLoaderCmd>("HotReload")
            .SetLargeImage("/RevitScript;component/Resources/Icons/RibbonIcon32.png");
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
        if (startupScript != null)
        {
            var executor = new ScriptExecutor(uiApplication);
            result = executor.ExecuteScript(startupScript);
            if (result == Result.Failed)
            {
                Debug.WriteLine($"Error Loading start up script {executor.Message}");
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