using System.IO;
using System.Reflection;
using System.Text;
using Autodesk.Revit.UI;
using IronPython.Compiler;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;


namespace RevitScript {
    // Executes a script
    public class ScriptExecutor(UIApplication uiApplication, bool fullFrame = false)
    {
        public string Message { get; private set; }

        public static string EnginePrefix => "";
        
        [PublicAPI]
        public static string EngineVersion {
            get {
                var assemblyVersion = Assembly.GetAssembly(typeof(ScriptExecutor)).GetName().Version;
                return $"{assemblyVersion.Minor}{assemblyVersion.Build}{assemblyVersion.Revision}";
            }
        }

        public Result ExecuteScript(string sourcePath,
                                    IEnumerable<string> sysPaths = null,
                                    string logFilePath = null,
                                    IDictionary <string, object> variables = null) {
            try {
                var engine = CreateEngine();
                var scope = SetupEnvironment(engine);

                // Add script directory address to sys search paths
                if (sysPaths != null) {
                    var path = engine.GetSearchPaths();
                    foreach (var sysPath in sysPaths)
                        path.Add(sysPath);

                    engine.SetSearchPaths(path);
                }


                // set globals
                scope.SetVariable("__file__", sourcePath);

                if (variables != null)
                    foreach(var keyPair in variables)
                        scope.SetVariable(keyPair.Key, keyPair.Value);

                //var script = engine.CreateScriptSourceFromString(source, SourceCodeKind.Statements);
                var script = engine.CreateScriptSourceFromFile(sourcePath, Encoding.UTF8, SourceCodeKind.Statements);

                // setting module to be the main module so __name__ == __main__ is True
                var compilerOptions = (PythonCompilerOptions)engine.GetCompilerOptions(scope);
                compilerOptions.ModuleName = "__main__";
                compilerOptions.Module |= IronPython.Runtime.ModuleOptions.Initialize;

                // Setting up an error reporter and compile the script
                var errors = new ErrorReporter();
                var command = script.Compile(compilerOptions, errors);
                if (command == null) {
                    // compilation failed, print errors and return
                    Message =
                        string.Join("\r\n", "IronPython Traceback:", string.Join("\r\n", errors.Errors.ToArray()));
                    if (logFilePath != null)
                        File.WriteAllText(logFilePath, Message);

                    return Result.Cancelled;
                }


                try {
                    script.Execute(scope);
                    return Result.Succeeded;
                }
                catch (SystemExitException) {
                    // ok, so the system exited. That was bound to happen...
                    return Result.Succeeded;
                }
                catch (Exception exception) {
                    string dotnetErrMessage = exception.ToString();
                    string ipyErrMessages = engine.GetService<ExceptionOperations>().FormatException(exception);

                    ipyErrMessages =
                        string.Join("\n", "IronPython Traceback:", ipyErrMessages.Replace("\r\n", "\n"));
                    dotnetErrMessage =
                        string.Join("\n", "Script Executor Traceback:", dotnetErrMessage.Replace("\r\n", "\n"));

                    Message = ipyErrMessages + "\n\n" + dotnetErrMessage;

                    // execution failed, log errors and return
                    if (logFilePath != null)
                        File.WriteAllText(logFilePath, Message);

                    return Result.Failed;
                }
                finally {
                    engine.Runtime.Shutdown();
                }

            }
            catch (Exception ex) {
                Message = ex.ToString();
                return Result.Failed;
            }
        }

        [PublicAPI]
        public ScriptEngine CreateEngine() {
            var flags = new Dictionary<string, object>
            {
                ["LightweightScopes"] = true
            };

            if (fullFrame) {
                flags["Frames"] = true;
                flags["FullFrames"] = true;
            }

            var engine = IronPython.Hosting.Python.CreateEngine(flags);

            return engine;
        }

        [PublicAPI]
        public void AddEmbeddedLib(ScriptEngine engine) {
            // use embedded python lib
            var asm = GetType().Assembly;
            string resName = $"python_{EngineVersion}_lib.zip";

            var resQuery = from name in asm.GetManifestResourceNames()
                where name.ToLowerInvariant().EndsWith(resName)
                select name;

            var importer = new IronPython.Modules.ResourceMetaPathImporter(asm, resQuery.Single());
            dynamic sys = IronPython.Hosting.Python.GetSysModule(engine);
            sys.meta_path.append(importer);
        }

        [PublicAPI]
        // Set up an IronPython environment
        public ScriptScope SetupEnvironment(ScriptEngine engine) {
            var scope = IronPython.Hosting.Python.CreateModule(engine, "__main__");

            SetupEnvironment(engine, scope);

            return scope;
        }

        [PublicAPI]
        public void SetupEnvironment(ScriptEngine engine, ScriptScope scope) {
            // add two special variables: __revit__ and __vars__ to be globally visible everywhere:            
            var builtin = IronPython.Hosting.Python.GetBuiltinModule(engine);
            builtin.SetVariable("__revit__", uiApplication);

            // add the search paths
            AddEmbeddedLib(engine);

            // reference RevitAPI and RevitAPIUI
            engine.Runtime.LoadAssembly(typeof(Document).Assembly);
            engine.Runtime.LoadAssembly(typeof(TaskDialog).Assembly);

            // also, allow access to the RPL internals
            engine.Runtime.LoadAssembly(typeof(ScriptExecutor).Assembly);
        }
    }

    public class ErrorReporter : ErrorListener {
        public readonly List<String> Errors = new();

        public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity) {
            Errors.Add($"{message} (line {span.Start.Line})");
        }

        public int Count => Errors.Count;
    }
}