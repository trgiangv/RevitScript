using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace RevitScript.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CPythonCmd : ExternalCommand
    {
        public override void Execute()
        {
            TaskDialog.Show("Warning", "This command is not implemented yet.");
        }
    }
}
