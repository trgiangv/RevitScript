using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;

namespace RevitScript.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class HotLoaderCmd : ExternalCommand
{
    public override void Execute()
    {
        try
        {
            LoadCommand();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }
    
    private const string AssemblyName = "DCMvn.Management";

    #region Commands

    private static void LoadCommand()
    {
        var folderPath = @"F:\DIG_GiangVu\workspace\DCMvn\source\DCMvn.Management\bin\Debug R22";
        var arrayByte = File.ReadAllBytes(Path.Combine(folderPath, $"{AssemblyName}.dll"));
        var assembly = Assembly.Load(arrayByte);

        if (assembly.GetType($"{AssemblyName}.FamilyParameter.Commands.TestCommand") is not { } type)
            return;

        if (type.GetMethod("Execute") is not { } methodInfo)
            return;

        var instance = Activator.CreateInstance(type, folderPath);
        methodInfo.Invoke(instance, null);
    }
    #endregion
}