using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BattleNET;
using BMRFME.VanillaPermissionProvider;
using BMRFME.Whitelist;
using BMRFME.Whitelist.Plugins;
using BMRFME.Whitelist.Sql;
using EasyConfigLib.Storage;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace BMRFME.VBulletinPermissionProvider
{
    public class VBulletinPermissionProvider
            : WhitelistPlugin
    {
        private VanillaConfig _config;

        private MySqlCommand _forumAccountQuery;
        private MySqlCommand _insertQuery;
        private MySqlCommand _loginQuery;
        private MySqlCommand _logoutQuery;
        private MySqlCommand _whitelistQuery;
        private MySqlCommand _forumGroupsQuery;
        private MySqlCommand _ghostingQuery;
        private MySqlCommand _checkBansQuery;
        private MySqlCommand _localBanQuery;
        private MySqlCommand _execBanQuery;


        private ResultSet _forumAccountResult;
        private ResultSet _forumGroupsResult;
        private ResultSet _whitelistResult;
        private ResultSet _ghostingResult;
        private ResultSet _checkBansResult;
        private ResultSet _localBanResult;

        private Connection _connection;
        private DateTime _lastConnection;
        private List<GUID> _checkedGuids;
        private ConnectionAttempts _connAttempts;


        public VBulletinPermissionProvider()
            : base("BMRF Whitelist Plugin", "5.0 Annoying Panda")
        {

        }

        public override bool ConsoleCtrlCheck(ConsoleControlHandler.CtrlTypes ctrlType)
        {
            Whitelister.Logger.Crash("Whitelisted Terminated.");
            Firewall.RemoveAllBans();
            return true;
        }

        public void GetGhosting(PlayerData player)
        {
            Whitelister.Logger.Debug("Get Ghosting {0}", player.Info.Name);
            if (player.GhostImmune)
            {
                player.IsGhosting = false;
                return;
            }

            _ghostingQuery.Parameters["@guid"].Value = player.Info.Guid;
            _ghostingResult = Connection.Query(_ghostingQuery);

            if (_ghostingResult.NumRows > 0)
            {
                var lastlogout = _ghostingResult[0].V<DateTime>("timestamp");
                var world = _ghostingResult[0].V<Int32>("world");
                var instance = _ghostingResult[0].V<Int32>("instance");
                if (_config.WorldNumber == world) //Staying within world
                {
                    if (_config.DayZInstance != instance) //changing instances
                    {
                        player.GhostAmount = new TimeSpan(0, _config.InstanceTimeout, 0) - (DateTime.Now - lastlogout);
                        if (player.GhostAmount.TotalMinutes >= 0)
                            player.IsGhosting = true;
                    }
                }
            }
            else
                player.IsGhosting = false;

        }

        public bool GetForumAccount(PlayerData player)
        {
            Whitelister.Logger.Debug("Get Forum Account {0}", player.Info.Name);
            _forumAccountQuery.Parameters["@addr"].Value = player.Info.IpAddr;
            _forumAccountResult = Connection.Query(_forumAccountQuery);

            if (_forumAccountResult.NumRows > 0)
            {
                player.HasForumAccount = true;
                player.ForumId = _forumAccountResult[0].V<Int32>("uid");
            }

            else if (player.ForumId != 0)
            {
                player.HasForumAccount = true;
            }
            else
            {
                player.HasForumAccount = false;
            }

            if (!_config.RequiredJoinGroups.Contains(0) && !player.HasForumAccount)
            {
                KickPlayer(player.Info, Whitelister.Instance.Config.KickMessage);
                return false;
            }
            return true;
        }

        public void GetForumGroups(PlayerData player)
        {
            Whitelister.Logger.Debug("Get Forum Group of {0}", player.Info.Guid);

            if (player.IsPublic)
            {
                player.PrimaryGroup = 0;
                player.SecondaryGroups.Clear();
            }

            if (player.ForumId != 0)
            {
                _forumGroupsQuery.Parameters["@userid"].Value = player.ForumId;
                _forumGroupsResult = Connection.Query(_forumGroupsQuery);
                player.PrimaryGroup = _forumGroupsResult[0].V<UInt16>("PrimaryGroup");
                player.SecondaryGroups.UnionWith(
                                               _forumGroupsResult[0].V<string>("SecondaryGroup")
                                                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                    .Select(int.Parse));
            }

            if (!_config.RequiredJoinGroups.Contains(0))
            {
                if (player.ForumId == 0)
                {
                    player.IsAllowed = false;
                    player.IsDisallowed = true;
                }
            }

            if (_config.DisallowedGroups.ContainsKey((int)player.PrimaryGroup) ||
                _config.DisallowedGroups.Keys.Any(x =>
                {
                    if (player.SecondaryGroups.Contains(x))
                    {
                        player.PrimaryGroup = x;
                        return true;
                    }
                    return false;
                }))
            {
                player.IsDisallowed = true;
                player.IsAllowed = false;
            }
            else if (_config.RequiredJoinGroups.Contains(player.PrimaryGroup) ||
                     _config.RequiredJoinGroups.Any(player.SecondaryGroups.Contains))
            {
                player.IsDisallowed = false;
                player.IsAllowed = true;
            }
            else
            {
                player.IsAllowed = false;
                player.IsDisallowed = false;
            }

            if (player.PrimaryGroup == _config.VipGroupId ||
                player.SecondaryGroups.Contains(_config.VipGroupId))
            {
                player.IsVip = true;
            }
            else
            {
                player.IsVip = false;
            }

            if (_config.GhostImmuneGroups.Contains(player.PrimaryGroup) ||
                _config.GhostImmuneGroups.Any(player.SecondaryGroups.Contains))
            {
                player.GhostImmune = true;
            }
            else
            {
                player.GhostImmune = false;
            }
        }


        public void GetWhitelist(PlayerData player)
        {
            Whitelister.Logger.Debug("Get Whitelist {0}", player.Info.Name);
            _whitelistQuery.Parameters["@guid"].Value = player.Info.Guid;
            _whitelistResult = Connection.Query(_whitelistQuery);

            player.IsPublic = _whitelistResult.NumRows == 0;
            if (player.IsPublic)
            {
                player.ForumId = 0;
                player.IsBanned = false;
            }
            else
            {
                player.ForumId = _whitelistResult[0].V<Int32>("uid");
                player.IsBanned = _whitelistResult[0].V<bool>("Banned");
                player.BanReason = player.IsBanned ? _whitelistResult[0].V<string>("BanReason") : "Not Banned";
            }
        }


        public void LoginPlayer(PlayerData player)
        {
            Whitelister.Logger.Info("{0} has logged in.", player.Info.Name);
            _loginQuery.Parameters["@name"].Value = player.Info.Name;
            _loginQuery.Parameters["@guid"].Value = player.Info.Guid;
            _loginQuery.Parameters["@ip"].Value = player.Info.IpAddr;
            _loginQuery.Parameters["@number"].Value = player.Info.Number;
            _loginQuery.Parameters["@world"].Value = _config.WorldNumber;
            _loginQuery.Parameters["@instance"].Value = _config.DayZInstance;
            Connection.Query(_loginQuery);
        }

        public void LogoutPlayer(PlayerData player)
        {
            Whitelister.Logger.Info("{0} has logged out.", player.Info.Name);
            _logoutQuery.Parameters["@guid"].Value = player.Info.Guid;
            _logoutQuery.Parameters["@ip"].Value = player.Info.IpAddr;
            _logoutQuery.Parameters["@instance"].Value = _config.DayZInstance;
            _logoutQuery.Parameters["@world"].Value = _config.WorldNumber;
            Connection.Query(_logoutQuery);
        }


        public void WhitelistPlayer(PlayerData player)
        {
            Whitelister.Logger.Debug("Whitelist Player {0}", player.Info.Name);
            _insertQuery.Parameters["@guid"].Value = player.Info.Guid;
            _insertQuery.Parameters["@name"].Value = player.Info.Name;
            _insertQuery.Parameters["@vuid"].Value = player.ForumId;
            Connection.Query(_insertQuery);
        }

        public void ExecBanQuery(string guid, string reason, string note)
        {
            Whitelister.Logger.Info("Banning GUID {0} >> {1}", guid, note);
            _execBanQuery.Parameters["@guid"].Value = guid;
            _execBanQuery.Parameters["@reason"].Value = reason;
            _execBanQuery.Parameters["@note"].Value = note;
            Connection.Query(_execBanQuery);
        }

        public void CheckBans(PlayerData player)
        {
            Whitelister.Logger.Debug("Check Bans {0}", player.Info.Name);
            _checkBansQuery.Parameters["@ipaddr"].Value = player.Info.IpAddr;
            _checkBansQuery.Parameters["@vuid"].Value = player.ForumId;
            _checkBansResult = Connection.Query(_checkBansQuery);

            if (_checkBansResult.NumRows == 0)
                return;

            var guidList = new List<GUID>();
            for (int i = 0; i < _checkBansResult.Count(); i++)
            {
                // is this necessary?
                //if (_checkBansResult[i].V<string>("guid") == player.Info.Guid)
                    guidList.Add(new GUID
                    {
                        Name = _checkBansResult[i].V<string>("guid"),
                        IsGlobalBanned = false,
                        IsLocalBanned = false
                    });
            }

            foreach (var guid in guidList)
            {
                guid.IsLocalBanned = CheckLocalBan(guid);
                if (!_checkedGuids.Contains(guid)) // We only want to check for global bans once
                {
                    guid.IsGlobalBanned = CheckGlobalBan(guid);
                    _checkedGuids.Add(guid);
                }
            }

            int index = guidList.FindIndex(guid => guid.IsGlobalBanned || guid.IsLocalBanned);
            if (index >= 0)
            {
                string note = String.Format("Linked to GUID {0}, which is {1}", guidList[index].Name,
                    guidList[index].IsGlobalBanned ? "global banned." : "local banned.");

                foreach (var guid in guidList)
                    ExecBanQuery(guid.Name, "You are linked to a previous ban, appeal on forums.", note);
                ExecBanQuery(player.Info.Guid, "You are linked to a previous ban, appeal on forums.", note);
            }
        }


        public void OnConnectAndList(PlayerData player, bool isConnection)
        {
            int watchdog = 0;

            //ohgodagoto:
            //    watchdog += 1;

            if (watchdog > 3)
            {
                Whitelister.Logger.Error("Watchdog triggered by {0}:{1}", player.Info.Name, player.Info.Guid);
                KickPlayer(player.Info, "Whitelister Error, Please Try Again");
                return;
            }

            if (isConnection && Whitelister.Instance.Config.FirewallEnabled) 
                SpamCheck(player);

            GetWhitelist(player);
            bool hasAccount = GetForumAccount(player);
            if (!hasAccount) return; // lol hack bad fix
            GetForumGroups(player);
            WhitelistPlayer(player);
            GetGhosting(player);
            CheckBans(player);

            if (player.IsAllowed && !(player.IsBanned || player.IsDisallowed))
            {
                if (player.IsGhosting)
                {
                    KickPlayer(player.Info, "You may change instances in {0:0.0} minutes", Math.Round(player.GhostAmount.TotalMinutes, 1));
                    return;
                }
                if (isConnection)
                {
                    if (Whitelister.CurrentPlayers.Count >= (_config.MaxServerSize - _config.VipSlots))
                    {
                        if (!player.IsVip)
                        {
                            KickPlayer(player.Info, _config.VipKickMessage);
                            return;
                        }
                    }

                    LoginPlayer(player);
                    return;
                }

                return;
            }
            if (player.IsDisallowed)
            {
                try
                {
                    // This will throw if the disallowed group is a secondary group.
                    // Should probably get the actual index properly here.
                    KickPlayer(player.Info, "{0}", _config.DisallowedGroups[player.PrimaryGroup]);
                    return;
                }
                catch (KeyNotFoundException)
                {
                    KickPlayer(player.Info, "One of your secondary groups is disallowed");
                    return;
                }
            }
            if (player.IsBanned)
            {
                KickPlayer(player.Info, "Banned-{0}", player.BanReason);
            }
        }

        public Connection Connection
        {
            get
            {
                if (DateTime.Now - _lastConnection > new TimeSpan(0, 0, 30))
                {
                    _connection.Disconnect();
                    _connection = new Connection(_config.AuthInfo);
                }
                return _connection;
            }
        }

        public void SpamCheck(PlayerData player)
        {
            int duration = Whitelister.Instance.Config.FirewallBanDuration;
            int rateLimit = Whitelister.Instance.Config.FirewallRateLimit;
            int maxAttempts = Whitelister.Instance.Config.FirewallMaxAttempts;

            _connAttempts.Add(player.Info.IpAddr);

            if (_connAttempts.Within(player.Info.IpAddr, rateLimit) >= maxAttempts)
            {
                KickPlayer(player.Info, "Too many join attempts. Firewalled for " 
                    + duration + " seconds");

                Firewall.AddBan(player.Info.IpAddr, 
                    String.Format("Spamming: {0}({1})", player.Info.Name, DateTime.Now),
                    duration);
            }
        }

        public static bool CheckGlobalBan(GUID guid)
        {
            IPAddress beMaster = Dns.GetHostAddresses("arma2oa1.battleye.com")[0];
            IPEndPoint beMasterEndPoint = new IPEndPoint(beMaster, 2324);
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    byte[] data = Encoding.ASCII.GetBytes("abcd" + guid.Name);
                    socket.SendTimeout = 500;
                    socket.ReceiveTimeout = 500;
                    socket.SendTo(data, beMasterEndPoint);

                    data = new byte[32];
                    int len = socket.Receive(data);
                    return len != 0 && len != 4;
                }
            }
            catch (Exception ex)
            {
                Whitelister.Instance.Logger.Error("Key {0} ban check fail: {1}", guid.Name, ex.Message);
                return false;
            }
        }

        public bool CheckLocalBan(GUID guid)
        {
            _localBanQuery.Parameters["@guid"].Value = guid.Name;
            _localBanResult = Connection.Query(_localBanQuery);
            if (_localBanResult.NumRows == 0)
                return false;
            return true;
        }

        public override void Setup()
        {
            _config = new VanillaConfig(string.IsNullOrWhiteSpace(Whitelister.PluginConfig) ? "vanilla_config.cfg" : Whitelister.PluginConfig);

            _forumAccountQuery = new MySqlCommand(_config.ForumAccountQuery);
            _forumAccountQuery.Parameters.AddWithValue("@addr", "");

            _loginQuery = new MySqlCommand(_config.LoginQuery);
            _loginQuery.Parameters.AddWithValue("@name", "");
            _loginQuery.Parameters.AddWithValue("@number", 0);
            _loginQuery.Parameters.AddWithValue("@guid", "");
            _loginQuery.Parameters.AddWithValue("@ip", "");
            _loginQuery.Parameters.AddWithValue("@instance", 0);
            _loginQuery.Parameters.AddWithValue("@world", 0);

            _logoutQuery = new MySqlCommand(_config.LogoutQuery);
            _logoutQuery.Parameters.AddWithValue("@guid", "");
            _logoutQuery.Parameters.AddWithValue("@instance", 0);
            _logoutQuery.Parameters.AddWithValue("@world", 0);
            _logoutQuery.Parameters.AddWithValue("@ip", "");

            _whitelistQuery = new MySqlCommand(_config.WhitelistQuery);
            _whitelistQuery.Parameters.AddWithValue("@guid", "");

            _insertQuery = new MySqlCommand(_config.InsertQuery);
            _insertQuery.Parameters.AddWithValue("@guid", "");
            _insertQuery.Parameters.AddWithValue("@name", "");
            _insertQuery.Parameters.AddWithValue("@vuid", 0);

            _forumGroupsQuery = new MySqlCommand(_config.ForumGroupQuery);
            _forumGroupsQuery.Parameters.AddWithValue("@userid", 0);

            _ghostingQuery = new MySqlCommand(_config.GhostingQuery);
            _ghostingQuery.Parameters.AddWithValue("@guid", "");
            _ghostingQuery.Parameters.AddWithValue("@world", _config.WorldNumber);

            _checkBansQuery = new MySqlCommand(_config.CheckBansQuery);
            _checkBansQuery.Parameters.AddWithValue("@ipaddr", "");
            _checkBansQuery.Parameters.AddWithValue("@vuid", "");

            _execBanQuery = new MySqlCommand(_config.ExecBanQuery);
            _execBanQuery.Parameters.AddWithValue("@guid", "");
            _execBanQuery.Parameters.AddWithValue("@reason", "");
            _execBanQuery.Parameters.AddWithValue("@note", "");

            _localBanQuery = new MySqlCommand(_config.CheckLocalBansQuery);
            _localBanQuery.Parameters.AddWithValue("@guid", "");

            _connection = new Connection(_config.AuthInfo);
            _lastConnection = DateTime.Now;

            _checkedGuids = new List<GUID>();
            _connAttempts = new ConnectionAttempts();

            Whitelister.PlayerListEvent += e => OnConnectAndList(new PlayerData(e.Player), false);
            Whitelister.PlayerDisconnectEvent += e => LogoutPlayer(new PlayerData(e.Player));
            Whitelister.PlayerConnectionEvent += e => OnConnectAndList(new PlayerData(e.Player), true);
        }



        public override void TearDown()
        {
            _connection = null;
        }

        public class PlayerData
        {
            public readonly PlayerInformation Info;

            public bool IsPublic = false;
            public bool IsBanned = false;
            public bool IsGhosting = false;
            public bool IsVip = false;
            public bool IsDisallowed = false;
            public bool IsAllowed = false;
            public bool GhostImmune = false;
            public bool HasForumAccount = false;

            public int PrimaryGroup = 0;
            public HashSet<int> SecondaryGroups = new HashSet<int>();

            public int ForumId = 0;

            public TimeSpan GhostAmount;

            public string BanReason = "None";


            public PlayerData(PlayerInformation info)
            {
                Info = new PlayerInformation(info.Name, info.IpAddr, info.Port.ToString(CultureInfo.InvariantCulture), info.Number.ToString(CultureInfo.InvariantCulture))
                {
                    Guid = info.Guid

                };
                GhostAmount = new TimeSpan(0, 0, 9001, 0);
            }
        }

        private class VanillaConfig
            : EasyConfig
        {

            [Field("", "DayZ Stuff")]
            public int DayZInstance = 1;

            [Field]
            public int WorldNumber = 1;

            [Field]
            public int InstanceTimeout = 10;

            [Field]
            public List<int> RequiredJoinGroups = new List<int>();

            [Field]
            public List<int> GhostImmuneGroups = new List<int>();

            [Field]
            public Dictionary<int, string> DisallowedGroups = new Dictionary<int, string>();

            [Field("Host", "MySQL Info")]
            public int MaxServerSize = 75;

            [Field]
            public int VipSlots = 15;

            [Field]
            public int VipGroupId = 0;

            [Field]
            public string VipKickMessage = "Only reserved slots left.";

            [Field]
            public string Address = "localhost";

            [Field]
            public int Port = 3316;

            [Field]
            public string User = "root";

            [Field]
            public string Pass = "root";

            [Field]
            public string Database = "vbulletin";

            [Field(Section = "Queries")]
            public string ForumAccountQuery =
                @"select u.userid as uid from vbulletin.user as u where u.ipaddress = @addr and u.lastactive > date_sub(now(), interval 24 hour)";

            [Field]
            public string ForumGroupQuery =
                @"select groupsstuff as PrimaryGroup, as SecondaryGroup from vbulletin.user where userid = @userid";

            [Field]
            public string LoginQuery =
                @"call bmrf.proc_login(@guid, @ipoaddr, @instance, @world, @number)";

            [Field]
            public string LogoutQuery =
                @"call bmrf.proc_logout(@guid, @ipaddr, @instance, @world)";

            [Field]
            public string GhostingQuery =
                @"select wrold, instance, `timestamp` as stuff from bmrf.login_history where action = 'logout' and guid = @guid order by timestamp desc limit 1";

            [Field]
            public string WhitelistQuery =
                @"select VUID as uid, GUID from bmrf.whitelist where GUID=@guid";

            [Field]
            public string InsertQuery =
                @"insert into bmrf.whitelist (GUID, PUID, VUID, Name) values(@guid, 0, @vuid, @name)";

            [Field]
            public string CheckBansQuery =
                @"select whitelist.guid from bmrf.whitelist inner join bmrf.login_history on whitelist.guid=login_history.guid where vuid = @vuid or ipaddr = @ipaddr";

            [Field]
            public string CheckLocalBansQuery =
                @"select * from whitelist where GUID = @guid and Banned = '1';";

            [Field]
            public string ExecBanQuery =
                @"update bmrf.whitelist set Banned = '1', BanReason = @reason, Note = @note where guid = @guid";

            public readonly ConnectionAuthInfo AuthInfo;

            public VanillaConfig(string filename)
                : base(filename)
            {
                AuthInfo = new ConnectionAuthInfo
                {
                    Hostname = Address,
                    Port = (int)Port,
                    Password = Pass,
                    User = User,
                    Database = Database
                };

            }
        }
    }
}