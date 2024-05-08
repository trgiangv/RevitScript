using System.IO;
using IronPython.Runtime;
using RevitScript.Runtime.Common;

namespace RevitScript.Runtime.Engine {
    public static class DomainStorageKeys {
        public static string keyPrefix = PyRevitLabsConsts.ProductName.ToUpperInvariant();

        public static string EnvVarsDictKey = keyPrefix + "EnvVarsDict";
        public static string EnginesDictKey = keyPrefix + "CachedEngines";
        public static string IronPythonEngineDefaultOutputStreamCfgKey = keyPrefix + "CachedEngineDefaultOutputStreamCfg";
        public static string IronPythonEngineDefaultInputStreamCfgKey = keyPrefix + "CachedEngineDefaultInputStreamCfg";
        public static string OutputWindowsDictKey = keyPrefix + "OutputWindowsDict";
    }

    public static class EnvDictionaryKeys
    {
        public static string keyPrefix = PyRevitLabsConsts.ProductName.ToUpperInvariant();

        public static string SessionUUID = $"{keyPrefix}_UUID";
        public static string RevitVersion = $"{keyPrefix}_APPVERSION";
        public static string Version = $"{keyPrefix}_VERSION";
        public static string Clone = $"{keyPrefix}_CLONE";
        public static string IPYVersion = $"{keyPrefix}_IPYVERSION";
        public static string CPYVersion = $"{keyPrefix}_CPYVERSION";

        public static string LoggingLevel = $"{keyPrefix}_LOGGINGLEVEL";
        public static string FileLogging = $"{keyPrefix}_FILELOGGING";

        public static string LoadedAssms = $"{keyPrefix}_LOADEDASSMS";
        public static string RefedAssms = $"{keyPrefix}_REFEDASSMS";

        public static string TelemetryState = $"{keyPrefix}_TELEMETRYSTATE";
        public static string TelemetryUTCTimeStamps = $"{keyPrefix}_TELEMETRYUTCTIMESTAMPS";
        public static string TelemetryFileDir = $"{keyPrefix}_TELEMETRYDIR";
        public static string TelemetryFilePath = $"{keyPrefix}_TELEMETRYFILE";
        public static string TelemetryServerUrl = $"{keyPrefix}_TELEMETRYSERVER";
        public static string TelemetryIncludeHooks = $"{keyPrefix}_TELEMETRYINCLUDEHOOKS";
        
        public static string AppTelemetryState = $"{keyPrefix}_APPTELEMETRYSTATE";
        public static string AppTelemetryHandler = $"{keyPrefix}_APPTELEMETRYHANDLER";
        public static string AppTelemetryServerUrl = $"{keyPrefix}_APPTELEMETRYSERVER";
        public static string AppTelemetryEventFlags = $"{keyPrefix}_APPTELEMETRYEVENTFLAGS";

        public static string Hooks = $"{keyPrefix}_HOOKS";
        public static string HooksHandler = $"{keyPrefix}_HOOKSHANDLER";

        public static string AutoUpdating = $"{keyPrefix}_AUTOUPDATE";
        public static string OutputStyleSheet = $"{keyPrefix}_STYLESHEET";
        public static string RibbonUpdator = $"{keyPrefix}_RIBBONUPDATOR";
        public static string TabColorizer = $"{keyPrefix}_TABCOLORIZER";
    }

    public class EnvDictionary
    {
        private PythonDictionary _envData = null;

        public string SessionUUID;
        public string RevitVersion;
        public string PyRevitVersion;
        public string PyRevitClone;
        public string PyRevitIPYVersion;
        public string PyRevitCPYVersion;

        public int LoggingLevel;
        public bool FileLogging;

        public string[] LoadedAssemblies;
        public string[] ReferencedAssemblies;

        public bool TelemetryState;
        public string TelemetryFilePath;
        public string TelemetryServerUrl;
        public bool TelemetryIncludeHooks;

        public bool AppTelemetryState;
        public string AppTelemetryServerUrl;
        public string AppTelemetryEventFlags;

        public Dictionary<string, Dictionary<string, string>> EventHooks = 
            new Dictionary<string, Dictionary<string, string>>();

        public string ActiveStyleSheet;
        public bool AutoUpdate;
        public bool TelemetryUTCTimeStamps;


        public EnvDictionary()
        {
            // get the dictionary from appdomain
            _envData = (PythonDictionary)AppDomain.CurrentDomain.GetData(DomainStorageKeys.EnvVarsDictKey);

            // base info
            if (_envData.Contains(EnvDictionaryKeys.SessionUUID))
                SessionUUID = (string)_envData[EnvDictionaryKeys.SessionUUID];

            if (_envData.Contains(EnvDictionaryKeys.RevitVersion))
                RevitVersion = (string)_envData[EnvDictionaryKeys.RevitVersion];

            if (_envData.Contains(EnvDictionaryKeys.Version))
                PyRevitVersion = (string)_envData[EnvDictionaryKeys.Version];

            if (_envData.Contains(EnvDictionaryKeys.Clone))
                PyRevitClone = (string)_envData[EnvDictionaryKeys.Clone];

            if (_envData.Contains(EnvDictionaryKeys.IPYVersion))
                PyRevitIPYVersion = (string)_envData[EnvDictionaryKeys.IPYVersion];

            if (_envData.Contains(EnvDictionaryKeys.CPYVersion))
                PyRevitCPYVersion = (string)_envData[EnvDictionaryKeys.CPYVersion];

            // logging
            if (_envData.Contains(EnvDictionaryKeys.LoggingLevel))
                LoggingLevel = (int)_envData[EnvDictionaryKeys.LoggingLevel];
            if (_envData.Contains(EnvDictionaryKeys.FileLogging))
                FileLogging = (bool)_envData[EnvDictionaryKeys.FileLogging];

            // assemblies
            if (_envData.Contains(EnvDictionaryKeys.LoadedAssms))
                LoadedAssemblies = ((string)_envData[EnvDictionaryKeys.LoadedAssms]).Split(Path.PathSeparator);
            if (_envData.Contains(EnvDictionaryKeys.RefedAssms))
                ReferencedAssemblies = ((string)_envData[EnvDictionaryKeys.RefedAssms]).Split(Path.PathSeparator);

            // telemetry
            if (_envData.Contains(EnvDictionaryKeys.TelemetryUTCTimeStamps))
                TelemetryUTCTimeStamps = (bool)_envData[EnvDictionaryKeys.TelemetryUTCTimeStamps];

            // script telemetry
            if (_envData.Contains(EnvDictionaryKeys.TelemetryState))
                TelemetryState = (bool)_envData[EnvDictionaryKeys.TelemetryState];

            if (_envData.Contains(EnvDictionaryKeys.TelemetryFilePath))
                TelemetryFilePath = (string)_envData[EnvDictionaryKeys.TelemetryFilePath];

            if (_envData.Contains(EnvDictionaryKeys.TelemetryServerUrl))
                TelemetryServerUrl = (string)_envData[EnvDictionaryKeys.TelemetryServerUrl];

            if (_envData.Contains(EnvDictionaryKeys.TelemetryIncludeHooks))
                TelemetryIncludeHooks = (bool)_envData[EnvDictionaryKeys.TelemetryIncludeHooks];

            // app events telemetry
            if (_envData.Contains(EnvDictionaryKeys.AppTelemetryState))
                AppTelemetryState = (bool)_envData[EnvDictionaryKeys.AppTelemetryState];

            if (_envData.Contains(EnvDictionaryKeys.AppTelemetryServerUrl))
                AppTelemetryServerUrl = (string)_envData[EnvDictionaryKeys.AppTelemetryServerUrl];

            if (_envData.Contains(EnvDictionaryKeys.AppTelemetryEventFlags))
                AppTelemetryEventFlags = (string)_envData[EnvDictionaryKeys.AppTelemetryEventFlags];

            // hooks
            if (_envData.Contains(EnvDictionaryKeys.Hooks))
                EventHooks = (Dictionary<string, Dictionary<string, string>>)_envData[EnvDictionaryKeys.Hooks];
            else
                _envData[EnvDictionaryKeys.Hooks] = EventHooks;

            // misc 
            if (_envData.Contains(EnvDictionaryKeys.AutoUpdating))
                AutoUpdate = (bool)_envData[EnvDictionaryKeys.AutoUpdating];

            if (_envData.Contains(EnvDictionaryKeys.OutputStyleSheet))
                ActiveStyleSheet = (string)_envData[EnvDictionaryKeys.OutputStyleSheet];

        }

        public void ResetEventHooks() {
            ((Dictionary<string, Dictionary<string, string>>)_envData[EnvDictionaryKeys.Hooks]).Clear();
        }
    }
}
