﻿using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IronPython.Runtime.Exceptions;
using JetBrains.Annotations;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace RpsRuntime
{
    /// <summary>
    /// Executes a script scripts
    /// </summary>
    public class ScriptExecutor
    {
        private readonly ExternalCommandData _commandData;
        private string _message;
        private readonly ElementSet _elements;
        private readonly UIApplication _revit;
        private readonly IRpsConfig _config;
        private readonly UIControlledApplication _uiControlledApplication;

        public ScriptExecutor(IRpsConfig config, UIApplication uiApplication, UIControlledApplication uiControlledApplication)
        {
            _config = config;

            _revit = uiApplication;
            _uiControlledApplication = uiControlledApplication;

            // note, if this constructor is used, then this stuff is all null
            // (I'm just setting it here to be explete - this constructor is
            // only used for the startup script)
            _commandData = null;
            _elements = null;
            _message = null;
        }

        public ScriptExecutor(IRpsConfig config, ExternalCommandData commandData, string message, ElementSet elements)
        {
            _config = config;

            _revit = commandData.Application;
            _commandData = commandData;
            _elements = elements;
            _message = message;

            _uiControlledApplication = null;
        }

        public string Message => _message;

        /// <summary>
        /// Run the script and print the output to a new output window.
        /// </summary>
        public int ExecuteScript(string source, string sourcePath)
        {
            try
            {
                var engine = CreateEngine();
                var scope = SetupEnvironment(engine);

                var scriptOutput = new ScriptOutput();
                scriptOutput.Show();
                var outputStream = new ScriptOutputStream(scriptOutput, engine);

                scope.SetVariable("__window__", scriptOutput);
                scope.SetVariable("__file__", sourcePath);

                // Add script directory address to sys search paths
                var path = engine.GetSearchPaths();
                path.Add(System.IO.Path.GetDirectoryName(sourcePath));
                engine.SetSearchPaths(path);

                engine.Runtime.IO.SetOutput(outputStream, Encoding.UTF8);
                engine.Runtime.IO.SetErrorOutput(outputStream, Encoding.UTF8);
                engine.Runtime.IO.SetInput(outputStream, Encoding.UTF8);

                var script = engine.CreateScriptSourceFromString(source, SourceCodeKind.Statements);
                var errors = new ErrorReporter();
                var command = script.Compile(errors);
                if (command == null)
                {
                    // compilation failed
                    _message = string.Join("\n", errors.Errors);
                    return (int)Result.Failed;
                }


                try
                {
                    script.Execute(scope);

                    _message = (scope.GetVariable("__message__") ?? "").ToString();
                    return (int)(scope.GetVariable("__result__") ?? Result.Succeeded);
                }
                catch (SystemExitException)
                {
                    // ok, so the system exited. That was bound to happen...
                    return (int)Result.Succeeded;
                }
                catch (Exception exception)
                {
                    // show (power) user everything!
                    _message = exception.ToString();
                    return (int)Result.Failed;
                }

            }
            catch (Exception ex)
            {
                _message = ex.ToString();
                return (int)Result.Failed;
            }
        }

        private ScriptEngine CreateEngine()
        {
            var engine = IronPython.Hosting.Python.CreateEngine(new Dictionary<string, object>() { { "Frames", true }, { "FullFrames", true } });                        
            return engine;
        }

        private void AddEmbeddedLib(ScriptEngine engine)
        {
            // use embedded python lib
            var asm = this.GetType().Assembly;
            string[] resourceNames = asm.GetManifestResourceNames();
            var resQuery = from name in resourceNames
                           where name.ToLowerInvariant().EndsWith("ironpython.stdlib.3.4.1.zip")
                           select name;
            var resName = resQuery.Single();
            var importer = new IronPython.Modules.ResourceMetaPathImporter(asm, resName);
            dynamic sys = IronPython.Hosting.Python.GetSysModule(engine);
            sys.meta_path.append(importer);            
        }

        /// <summary>
        /// Set up an IronPython environment - for interactive shell or for canned scripts
        /// </summary>
        [PublicAPI]
        public ScriptScope SetupEnvironment(ScriptEngine engine)
        {
            var scope = IronPython.Hosting.Python.CreateModule(engine, "__main__");

            SetupEnvironment(engine, scope);

            return scope;
        }

        public void SetupEnvironment(ScriptEngine engine, ScriptScope scope)
        {
            // these variables refer to the signature of the IExternalCommand.Execute method
            scope.SetVariable("__commandData__", _commandData);
            scope.SetVariable("__message__", _message);
            scope.SetVariable("__elements__", _elements);
            scope.SetVariable("__result__", (int)Result.Succeeded);

            // add two special variables: __revit__ and __vars__ to be globally visible everywhere:            
            var builtin = IronPython.Hosting.Python.GetBuiltinModule(engine);
            builtin.SetVariable("__revit__", _revit);
            builtin.SetVariable("__vars__", _config.GetVariables());

            // allow access to the UIControlledApplication in the startup script...
            if (_uiControlledApplication != null)
            {
                builtin.SetVariable("__uiControlledApplication__", _uiControlledApplication);
            }
            
            // add the search paths
            AddSearchPaths(engine);
            AddEmbeddedLib(engine);

            // reference RevitAPI and RevitAPIUI
            engine.Runtime.LoadAssembly(typeof(Document).Assembly);
            engine.Runtime.LoadAssembly(typeof(TaskDialog).Assembly);

            // also, allow access to the RPS internals
            engine.Runtime.LoadAssembly(typeof(ScriptExecutor).Assembly);
        }

        /// <summary>
        /// Add the search paths defined in the ini file to the engine.
        /// The data folder (%APPDATA%/RevitPythonShell20XX) is also added
        /// </summary>
        private void AddSearchPaths(ScriptEngine engine)
        {
            var searchPaths = engine.GetSearchPaths();
            foreach (var path in _config.GetSearchPaths())
            {
                searchPaths.Add(path);
            }
            engine.SetSearchPaths(searchPaths);
        }
    }


    public class ErrorReporter : ErrorListener
    {
        public readonly List<String> Errors = [];

        public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
        {
            Errors.Add($"{message} (line {span.Start.Line})");
        }
    }
}