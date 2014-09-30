using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;
using BMRFME.Whitelist;
using BMRFME.Whitelist.Plugins;
using MySql.Data.MySqlClient;

namespace BMRFME.PermissionScriptingPlugin
{
    public class ScriptingPlugin
        : WhitelistPlugin
    {
        private CodeDomProvider _compiler;
        private Assembly _compiledScript;
        private ScriptingPluginConfig _config;
        private WhitelistPlugin _pluginProxy;

        public ScriptingPlugin()
            : base("Scripting Plugin", "0.5a")
        {

        }

        public override void Setup()
        {
            _config = new ScriptingPluginConfig();

            if (!CodeDomProvider.IsDefinedLanguage("C#"))
                throw new Exception("C# Compiler not found");

            _compiler = CodeDomProvider.CreateProvider("C#");

            var options = new CompilerParameters();
            var myass = Assembly.GetEntryAssembly();

            options.ReferencedAssemblies.Add(myass.Location);
            var others = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var ass in others)
            {
                options.ReferencedAssemblies.Add(ass.Location);
            }

            foreach (var ass in _config.ExternalDlls)
            {
                options.ReferencedAssemblies.Add(Assembly.LoadFile(ass).Location);
            }

            options.GenerateExecutable = _config.SaveAssembly;

            if (_config.SaveAssembly)
            {
                options.OutputAssembly = Path.Combine("scripts", _config.Filename + ".dll");
                options.GenerateInMemory = false;
            }
            else
                options.GenerateInMemory = true;


            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "scripts")))
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "scripts"));

            Whitelister.Logger.Info("Compiling Script File");
            var results = _compiler.CompileAssemblyFromFile(options,
                                              Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "scripts", _config.Filename)));

            if (results.Errors.HasErrors)
            {
                Whitelister.Logger.Crash("Errors found while compiling script!");
                foreach (CompilerError error in results.Errors)
                {
                    Whitelister.Logger.Info("\t{0} Line {1} \"{2}\"", (error.IsWarning) ? "WARNING" : "ERROR", error.Line,
                                                    error.ErrorText);
                }

                throw new Exception("Errors found while compiling script");
            }

            if (results.Errors.HasWarnings)
            {
                Whitelister.Logger.Warn("Warnings found while compiling script!");
                foreach (CompilerError error in results.Errors)
                {
                    Whitelister.Logger.Info("\t{0} Line {1} \"{2}\"", (error.IsWarning) ? "WARNING" : "ERROR", error.Line,
                                                     error.ErrorText);
                }
            }

            _compiledScript = results.CompiledAssembly;


            Type[] types = _compiledScript.GetExportedTypes();
            for (int n = 0; n < types.Length; n++)
            {
                Type type = types[n];
                if (type.BaseType == typeof(WhitelistPlugin))
                {
                    _pluginProxy = (WhitelistPlugin)Activator.CreateInstance(type);
                    break;
                }
            }

            if (_pluginProxy == null)
            {
                Whitelister.Logger.Crash("Script does not contain whitelist plugin");
                throw new Exception("Unable to load compiled whitelist plugin script.");
            }

            Whitelister.Logger.Info("Executing {0}.Setup", _pluginProxy.Name);

            try
            {
                _pluginProxy.Whitelister = Whitelister;
                _pluginProxy.Setup();
            }
            catch (Exception e)
            {
                Whitelister.Logger.Crash("Exception while running {0}.Setup", _pluginProxy.Name);
                throw new Exception("Exception while running proxyd Plugin Setup", e);
            }

        }

        public override void TearDown()
        {
            _pluginProxy.TearDown();
        }
    }
}
