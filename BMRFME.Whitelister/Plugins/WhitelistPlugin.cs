using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BMRFME.Whitelist.Events;

namespace BMRFME.Whitelist.Plugins
{
    public abstract class WhitelistPlugin
    {
        public readonly string Name;
        public readonly string Version;
        public Whitelister Whitelister;

        protected WhitelistPlugin(string name, string version)
        {
            Name = name;
            Version = version;
            ConsoleControlHandler.SetConsoleCtrlHandler((ConsoleCtrlCheck), true);
        }

        public void KickPlayer(PlayerInformation playerArgs, string format, params object[] args)
        {
            var formatedString = "";
            if (args.Length > 0)
                formatedString = string.Format(format, args);
            else
                formatedString = format;

            if (formatedString.Length >= 80)
                formatedString = formatedString.Substring(0, 79);
            PlayerInformation outInfo;
            Whitelister.CurrentPlayers.TryRemove(playerArgs.Number, out outInfo);

            Whitelister.Logger.Info("Sending BEMessage - kick {0} {1}", playerArgs.Number, formatedString);

            Whitelister.Client.SendCommand(BattleNET.BattlEyeCommand.Kick,
                string.Format("{0} {1}", playerArgs.Number, formatedString));
        }

        public void BanPlayer(PlayerInformation playerArgs, string format, params object[] args)
        {
            var formatedString = string.Format(format, args);
            if (formatedString.Length >= 80)
                formatedString = formatedString.Substring(0, 79);
            Whitelister.Client.SendCommand(BattleNET.BattlEyeCommand.Ban,
                string.Format("{0} {1}", playerArgs.Number, formatedString));
        }

        public virtual bool ConsoleCtrlCheck(ConsoleControlHandler.CtrlTypes ctrlType)
        {
            // Put your own handler here
            return true;
        }

        public static WhitelistPlugin LoadPluginFromFile(string fullpath)
        {
            string currentDirectory = Path.GetDirectoryName(fullpath);

            if (currentDirectory == "None")
                return null;

            if (!File.Exists(fullpath))
                return null;

            ResolveEventHandler assemblyResolver = delegate(object sender, ResolveEventArgs args)
            {
                string assemblyName = Path.Combine(currentDirectory, args.Name);
                return File.Exists(assemblyName) ? Assembly.LoadFrom(assemblyName) : null;
            };

            WhitelistPlugin plugin = null;

            var ass = Assembly.LoadFrom(fullpath);
            Type[] types = ass.GetExportedTypes();
            for (int n = 0; n < types.Length; n++)
            {
                Type type = types[n];
                if (type.BaseType == typeof(WhitelistPlugin))
                {
                    plugin = (WhitelistPlugin)Activator.CreateInstance(type);
                    break;
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolver;

            return plugin;
        }

        public abstract void Setup();
        public abstract void TearDown();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PluginInfoAttribute
        : Attribute
    {
        public string Name;
        public string Version;
    }
}
