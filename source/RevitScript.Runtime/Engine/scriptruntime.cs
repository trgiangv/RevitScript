using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using RevitScript.Runtime.Common;

namespace RevitScript.Runtime.Engine {
    public enum ScriptRuntimeType {
        ExternalCommand,
        EventHandler,
    }

    public class ScriptRuntimeConfigs : IDisposable {
        private object _eventSender = null;

        public ControlledApplication ControlledApp { get; set; }
        public Application App { get; set; }
        public UIControlledApplication UIControlledApp { get; set; }
        public UIApplication UIApp { get; set; }

        public ExternalCommandData CommandData { get; set; }
        public ElementSet SelectedElements { get; set; }

        public List<string> SearchPaths { get; set; }
        public List<string> Arguments { get; set; }

        public object EventSender {
            get => _eventSender;
            set
            {
                if (value == null) return;
                // determine sender type
                if (value.GetType() == typeof(UIControlledApplication))
                    _eventSender = (UIControlledApplication)value;
                else if (value.GetType() == typeof(UIApplication))
                    _eventSender = (UIApplication)value;
                else if (value.GetType() == typeof(ControlledApplication))
                    _eventSender = (ControlledApplication)value;
                else if (value.GetType() == typeof(Application))
                    _eventSender = (Application)value;
            }
        }
        public object EventArgs { get; set; }

        public string EngineConfigs;

        public bool RefreshEngine;
        public bool ConfigMode;
        public bool DebugMode;
        public bool ExecutedFromUI;

        public void Dispose() {
            CommandData = null;
            SelectedElements = null;
            SearchPaths = null;
            Arguments = null;
            EventSender = null;
            EventArgs = null;
        }
    }

    public class ScriptRuntime : IDisposable {
        // app handles
        private UIApplication _uiApp;
        private Application _app;

        // output window and stream
        private WeakReference<ScriptConsole> _scriptOutput = new(null);
        private WeakReference<ScriptIO> _outputStream = new(null);

        // dict for command result data
        private Dictionary<string, string> _resultsDict = null;

        public ScriptRuntime(ScriptData scriptData, ScriptRuntimeConfigs scriptRuntimeCfg) {
            // setup a new id and timestamp
            ExecId = CommonUtils.NewShortUUID();
            ExecTimestamp = Telemetry.GetTelemetryTimeStamp();

            // set data
            ScriptData = scriptData;
            ScriptRuntimeConfigs = scriptRuntimeCfg;

            //env
            // get the state of variables before command execution; the command could potentially change the values
            EnvDict = new EnvDictionary();

            // apply application override hooks if given
            if (ScriptRuntimeConfigs.ControlledApp != null)
                ControlledApp = ScriptRuntimeConfigs.ControlledApp;
            if (ScriptRuntimeConfigs.App != null)
                App = ScriptRuntimeConfigs.App;
            if (ScriptRuntimeConfigs.UIControlledApp != null)
                UIControlledApp = ScriptRuntimeConfigs.UIControlledApp;
            if (ScriptRuntimeConfigs.UIApp != null)
                UIApp = ScriptRuntimeConfigs.UIApp;

            // determine event sender type
            if (ScriptRuntimeConfigs.EventSender != null) {
                // detemine sender type
                if (ScriptRuntimeConfigs.EventSender.GetType() == typeof(UIControlledApplication))
                    UIControlledApp = (UIControlledApplication)ScriptRuntimeConfigs.EventSender;
                else if (ScriptRuntimeConfigs.EventSender.GetType() == typeof(UIApplication))
                    UIApp = (UIApplication)ScriptRuntimeConfigs.EventSender;
                else if (ScriptRuntimeConfigs.EventSender.GetType() == typeof(ControlledApplication))
                    ControlledApp = (ControlledApplication)ScriptRuntimeConfigs.EventSender;
                else if (ScriptRuntimeConfigs.EventSender.GetType() == typeof(Application))
                    App = (Application)ScriptRuntimeConfigs.EventSender;
            }

            // prepare results
            ExecutionResult = ScriptExecutorResultCodes.Succeeded;
            TraceMessage = string.Empty;
        }

        public ScriptData ScriptData { get; private set; }
        public ScriptRuntimeConfigs ScriptRuntimeConfigs { get; private set; }

        // target script
        public string ScriptSourceFile {
            get {
                if (ScriptRuntimeConfigs.ConfigMode && (ScriptData.ConfigScriptPath != null || ScriptData.ConfigScriptPath != string.Empty))
                    return ScriptData.ConfigScriptPath;
                else
                    return ScriptData.ScriptPath;
            }
        }

        public string ScriptSourceFileSignature => CommonUtils.GetFileSignature(ScriptSourceFile);

        public ScriptRuntimeType RuntimeType {
            get {
                if (ScriptRuntimeConfigs.EventSender != null || ScriptRuntimeConfigs.EventArgs != null)
                    return ScriptRuntimeType.EventHandler;

                return ScriptRuntimeType.ExternalCommand;
            }
        }

        public ScriptEngineType EngineType {
            get {
                // determine engine necessary to run this script

                if (PyRevitScript.IsType(ScriptSourceFile, PyRevitScriptTypes.Python)) {
                    if (File.Exists(ScriptSourceFile))
                    {
                        using StreamReader reader = new StreamReader(ScriptSourceFile);
                        
                        return ScriptEngineType.IronPython;
                    }
                }

                else if (PyRevitScript.IsType(ScriptSourceFile, PyRevitScriptTypes.CSharp)) {
                    return ScriptEngineType.CSharp;
                }

                else if (PyRevitScript.IsType(ScriptSourceFile, PyRevitScriptTypes.Dynamo)) {
                    return ScriptEngineType.DynamoBIM;
                }

                if (ScriptData.CommandBundle == null) return ScriptEngineType.Unknown;
                return PyRevitBundle.IsType(ScriptData.CommandBundle, PyRevitBundleTypes.InvokeButton) ? ScriptEngineType.Invoke :
                    // if the script is deleted during runtime
                    // ScriptSourceFile with be "" and runtime can not determine
                    // the engine type
                    ScriptEngineType.Unknown;
            }
        }

        public string EngineVersion {
            get {
                switch (EngineType) {
                    case ScriptEngineType.IronPython: return EnvDict.PyRevitIPYVersion;
                    case ScriptEngineType.CSharp: return EnvDict.PyRevitVersion;
                    case ScriptEngineType.Invoke: return EnvDict.PyRevitVersion;
                    case ScriptEngineType.DynamoBIM: return EnvDict.PyRevitVersion;
                    case ScriptEngineType.Content: return EnvDict.PyRevitVersion;
                    default: return EnvDict.PyRevitVersion;
                }
            }
        }

        public Autodesk.Windows.RibbonItem UIControl => ((UIFramework.RevitRibbonControl)Autodesk.Windows.ComponentManager.Ribbon).findRibbonItem(ScriptData.CommandControlId, false);

        // environment
        // pyrevit
        public EnvDictionary EnvDict { get; set; }

        public string PyRevitVersion => EnvDict.PyRevitVersion;

        public string CloneName => EnvDict.PyRevitClone;

        public string SessionUUID => EnvDict.SessionUUID;

        // revit
        public string DocumentName {
            get {
                if (UIApp != null && UIApp.ActiveUIDocument != null)
                    return UIApp.ActiveUIDocument.Document.Title;
                else
                    return string.Empty;
            }
        }

        public string DocumentPath {
            get {
                if (UIApp != null && UIApp.ActiveUIDocument != null)
                    return UIApp.ActiveUIDocument.Document.PathName;
                else
                    return string.Empty;
            }
        }

        public ControlledApplication ControlledApp { get; set; }

        public Application App {
            get {
                if (ScriptRuntimeConfigs.CommandData != null)
                    return ScriptRuntimeConfigs.CommandData.Application.Application;
                else if (UIApp != null)
                    return UIApp.Application;
                else if (_app != null)
                    return _app;
                return null;
            }

            set => _app = value;
        }

        public UIControlledApplication UIControlledApp { get; set; }

        public UIApplication UIApp {
            get {
                if (ScriptRuntimeConfigs.CommandData != null)
                    return ScriptRuntimeConfigs.CommandData.Application;
                else if (_uiApp != null)
                    return _uiApp;
                return null;
            }

            set => _uiApp = value;
        }

        // output
        public ScriptConsole OutputWindow {
            get {
                // get ScriptOutput from the weak reference
                var re = _scriptOutput.TryGetTarget(out var output);
                if (re)
                    return output;
                // Stating a new output window
                var newOutput = new ScriptConsole(ScriptRuntimeConfigs.DebugMode, UIApp);

                // Set output window title to command name
                newOutput.OutputTitle = ScriptData.CommandName;

                // Set window identity to the command unique identifier
                newOutput.OutputId = ScriptData.CommandUniqueId;

                // set window app version header
                newOutput.AppVersion = string.Format(
                    "{0}:{1}:{2}",
                    EnvDict.PyRevitVersion,
                    EnvDict.PyRevitIPYVersion,
                    EnvDict.RevitVersion
                );

                _scriptOutput = new WeakReference<ScriptConsole>(newOutput);
                return newOutput;
            }
        }

        public ScriptIO OutputStream {
            get {
                // get ScriptOutputStream from the weak reference
                var re = _outputStream.TryGetTarget(out var outputStream);
                if (re)
                    return outputStream;
                // Set up the output stream
                ScriptIO newStream = new ScriptIO(this);
                _outputStream = new WeakReference<ScriptIO>(newStream);
                return newStream;
            }
        }

        // execution
        public string ExecId { get; private set; }

        public string ExecTimestamp { get; private set; }

        public int ExecutionResult { get; set; }

        public string TraceMessage { get; set; }

        public Dictionary<string, string> GetResultsDictionary() {
            if (_resultsDict == null)
                _resultsDict = new Dictionary<string, string>();

            return _resultsDict;
        }

        // disposal
        public void Dispose() {
            UIControlledApp = null;
            ControlledApp = null;
            _uiApp = null;
            _app = null;
            _scriptOutput = new WeakReference<ScriptConsole>(null);
            _outputStream = new WeakReference<ScriptIO>(null);;
            _resultsDict = null;
        }
    }
}
