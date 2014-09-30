using System.Collections.Generic;
using EasyConfigLib.Storage;

namespace BMRFME.PermissionScriptingPlugin
{
    public class ScriptingPluginConfig
        : EasyConfig
    {
        public ScriptingPluginConfig()
            : base("scripting-plugin.cfg")
        {
        }

        [Field("File Name", "Config")]
        public string Filename = "script.cs";

        [Field("External DLL's")]
        public List<string> ExternalDlls = new List<string>();

        [Field("Save Assembly")]
        public bool SaveAssembly = false;
    }
}
