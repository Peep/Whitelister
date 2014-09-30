using System;
using System.Collections.Generic;
using System.IO;
using BMRFME.Whitelist;
using BMRFME.Whitelist.Plugins;
using BMRFME.Whitelist.Events;
using EasyConfigLib.Storage;

/**
 * Example Whitelister plugin.
 * The following DLL's are guaranteed to be available. 
 * 
 * MySql.Data.dll
 * IniParser.dll
 * BattleNET.dll
 * 
 * Any other dll's must be included with your dll. 
 * Either by embedding them or packaging them with your release.
 * 
 */

namespace BMRFME.TextPermissionProvider
{
    public class TextPermissionProvider
        : WhitelistPlugin
    {
        private readonly TextConfig _config;
        private readonly DateTime _lastCheck;
        private readonly HashSet<string> _permissionList;

        public TextPermissionProvider()
            : base("Text Permissions", "1.4")
        {
            _config = new TextConfig();
            _permissionList = new HashSet<string>();
            _lastCheck = DateTime.Now;
        }

        public override void Setup()
        {
            Whitelister.PlayerConnectionEvent += CanJoin;
            Whitelister.PlayerListEvent += CanJoin;
        }

        public override void TearDown()
        {

        }

        private void CanJoin(PlayerEventArgs e)
        {
            switch (e.Type)
            {
                case PlayerEventArgs.EventType.Connect:
                    {
                        var info = e.Player;
                        if (!_permissionList.Contains(info.Guid))
                            KickPlayer(e.Player, Whitelister.Instance.Config.KickMessage);
                        break;
                    }
                case PlayerEventArgs.EventType.List:
                    {
                        var info = e.Player;
                        if (!_permissionList.Contains(info.Guid))
                            KickPlayer(e.Player, Whitelister.Instance.Config.KickMessage);
                        break;
                    }
            }
        }

        private void refreshList()
        {
            if ((DateTime.Now - _lastCheck).TotalMilliseconds
                < _config.RefreshRate)
                return;

            _permissionList.Clear();
            using (StreamReader file = File.OpenText(_config.PermissionFile))
            {
                while (!file.EndOfStream)
                {
                    string line = file.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) break;
                    _permissionList.Add(line);
                }
            }
        }

        #region Nested type: TextConfig

        private class TextConfig
            : EasyConfig
        {
            [Field(Name = "File", Section = "General")]
            public string PermissionFile = "whitelist.txt";

            [Field(Name = "RefreshRate")]
            public int RefreshRate = 5000;

            public TextConfig()
                : base("text_config.cfg")
            {
            }
        }

        #endregion
    }
}
