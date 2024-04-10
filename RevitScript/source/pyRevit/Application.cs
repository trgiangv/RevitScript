using Nice3point.Revit.Toolkit.External;
using pyRevit.Commands;

namespace pyRevit;

/// <summary>
///     Application entry point
/// </summary>
[UsedImplicitly]
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        Host.Start();
        CreateRibbon();
    }

    public override void OnShutdown()
    {
        Host.Stop();
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("Commands", "pyRevit");

        panel.AddPushButton<StartupCommand>("Execute")
            .SetImage("/pyRevit;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/pyRevit;component/Resources/Icons/RibbonIcon32.png");
    }
}