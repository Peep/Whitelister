using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using BattleNET;
using BMRFME.Whitelist.Events;
using BMRFME.Whitelist.Plugins;
using System.Net;
using EasyConfigLib.Storage;

namespace BMRFME.Whitelist
{
    public class Whitelister
    {
        public static readonly Whitelister Instance;
        public string PluginConfig;

        public readonly ConfigWhitelist Config;
        public readonly RegExp Expressions;
        public readonly Logger Logger;
        private readonly BattlEyeLoginCredentials _clientAuth;

        public int NumCurrentPlayers
        {
            get { return CurrentPlayers.Count; }
        }

        private readonly Dictionary<string, PlayerInformation> _connectStaging;

        private readonly Queue<PlayerEventArgs> _consumerEventQueue;
        private readonly Queue<PlayerEventArgs> _producerEventQueue;
        private readonly AutoResetEvent _queueEvent;

        private readonly string _pluginDirectory = Path.Combine(Environment.CurrentDirectory, "plugins");

        public WhitelistPlugin Plugin;
        public readonly ConcurrentDictionary<int, PlayerInformation> CurrentPlayers;
        public List<PlayerMessage> ChatMessages;

        private BattlEyeClient _client;
        private bool _running;

        static Whitelister()
        {
            Instance = new Whitelister();
            Instance.Plugin = WhitelistPlugin.LoadPluginFromFile(Path.Combine(Instance._pluginDirectory, Instance.Config.Plugin));

            if (Instance.Plugin == null)
                Instance.Logger.Error("Unable to load plugin from file {0}", Instance.Config.Plugin);

            if (Instance.Plugin != null)
            {

                Instance.Logger.Info("Loaded Plugin {0} Version {1}", Instance.Plugin.Name, Instance.Plugin.Version);
                Instance.Plugin.Whitelister = Instance;
                Instance.Plugin.Setup();
            }
        }

        private Whitelister()
        {
            try
            {
                Config = new ConfigWhitelist(Environment.GetCommandLineArgs()[1]);
                PluginConfig = Environment.GetCommandLineArgs()[2];
                Logger = new Logger("whitelist.log")
                {
                    LogToConsole = Config.LogToConsole
                };

                //Logger.Level = Logger.LogLevel.Debug;
                Expressions = new RegExp(Config);
                _connectStaging = new Dictionary<string, PlayerInformation>(20);
                CurrentPlayers = new ConcurrentDictionary<int, PlayerInformation>();
                ChatMessages = new List<PlayerMessage>();
                _producerEventQueue = new Queue<PlayerEventArgs>(20);
                _consumerEventQueue = new Queue<PlayerEventArgs>(20);
                _queueEvent = new AutoResetEvent(false);

                if (!Directory.Exists(_pluginDirectory))
                    Directory.CreateDirectory(_pluginDirectory);

                _clientAuth = new BattlEyeLoginCredentials(IPAddress.Parse(Config.BattlEyeAddr), Config.BattlEyePort, Config.BattlEyePass);
                _client = new BattlEyeClient(_clientAuth);
                _client.ReconnectOnPacketLoss = false;
                _client.BattlEyeMessageReceived += MessageRecievedEvent;
                _client.BattlEyeDisconnected += args => this.Stop();
                _client.Connect();
            }
            catch (Exception ex)
            {
                if (Logger != null)
                    Logger.Crash("Fatal error during startup. \n\t{0}", ex.Message);
                throw new TypeInitializationException(typeof(Whitelister).FullName, ex);
            }
        }

        public BattlEyeClient Client
        {
            get
            {
                if (_client.Connected)
                    return _client;

                Logger.Warn("BEClient was disconnected, trying reconnect");
                Instance.Stop();
                System.Environment.Exit(-1);
                _client.BattlEyeMessageReceived -= MessageRecievedEvent;
                _client = new BattlEyeClient(_clientAuth);
                _client.BattlEyeMessageReceived += MessageRecievedEvent;
                _client.ReconnectOnPacketLoss = false;
                _client.Connect();

                Thread.Sleep(4000); //HACK Ugh BAD FIX

                if (!_client.Connected)
                {
                    Logger.Warn("BEClient still disconnected!");
                    _client.Disconnect();
                    Instance.Stop();

                }
                else
                    Logger.Info("BEClient reconnected!");



                return _client;
            }
        }

        public void MessageRecievedEvent(BattlEyeMessageEventArgs e)
        {
            //Logger.Debug("Message received: " + e.Message);
            //TODO Refactor
            string[] matches = Expressions.PlayerConnectStg1.Split(e.Message);
            if (matches.Length == 6)
            {
                Logger.Debug("Got PlayerInfo from Connect Stage 1");
                //TODO Use Assositive Names
                var info = new PlayerInformation(name: matches[2], number: matches[1], ipaddr: matches[3], port: matches[4]);
                _connectStaging.Add(info.Name, info);
                Logger.Debug("\tName:{0}\n\tNumber:{1}\n\tPort:{2}\n\tIP:{3}",
                             matches[2], matches[1], matches[4], matches[3]);
                return;
            }

            matches = Expressions.PlayerConnectStg2.Split(e.Message);
            if (matches.Length == 5)
            {
                Logger.Debug("Got PlayerInfo from Connect Stage 2");
                PlayerInformation info;

                if (!_connectStaging.TryGetValue(matches[2], out info))
                    return;

                info.Guid = matches[3];
                _connectStaging.Remove(info.Name);
                lock (_producerEventQueue)
                {
                    _producerEventQueue.Enqueue(new PlayerConnectEventArgs(info));
                }

                Logger.Debug("\tName:{0}\n\tNumber:{1}\n\tPort:{2}\n\tIP:{3}\nGUID:{4}",
                             info.Name, info.Number, info.Port, info.IpAddr, info.Guid);

                _queueEvent.Set();
                return;
            }

            matches = Expressions.PlayerListHeader.Split(e.Message);
            if (matches.Length == 2)
            {
                //CurrentPlayers.Clear();
                var r = new StringReader(matches[1]);
                //Throw away first three header lines.
                r.ReadLine();
                r.ReadLine();
                r.ReadLine();

                lock (CurrentPlayers)
                {
                    CurrentPlayers.Clear();
                    while (true)
                    {
                        string line = r.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            break;

                        string[] m = Expressions.PlayerListValue.Split(line);
                        if (m.Length == 8)
                        {
                            var info = new PlayerInformation(name: m[6], number: m[1], ipaddr: m[2], port: m[3]);
                            info.Guid = m[5];

                            Logger.Debug("Got Player Info");
                            Logger.Debug("\tName:{0}\n\tNumber:{1}\n\tPort:{2}\n\tIP:{3}",
                                         info.Name, info.Number, info.Port, info.IpAddr);

                            CurrentPlayers[info.Number] = info;
                            lock (_producerEventQueue)
                            {
                                _producerEventQueue.Enqueue(new PlayerListEventArgs(info));
                            }
                        }
                    }
                    r.Close();
                }

                _queueEvent.Set();
                return;
            }

            matches = Expressions.PlayerDisconnect.Split(e.Message);
            if (matches.Length == 4)
            {
                var info = new PlayerInformation(name: matches[2], number: matches[1], ipaddr: "0.0.0.0", port: "0");
                if (!CurrentPlayers.TryGetValue(info.Number, out info))
                {
                    Logger.Warn("Unable to get player info from data store. Please report on github");
                }


                Logger.Debug("Got Player Disconnect");
                Logger.Debug("\tName : {0}", info.Name);

                lock (_producerEventQueue)
                {
                    _producerEventQueue.Enqueue(new PlayerDisconnectEventArgs(info));
                }
                CurrentPlayers.TryRemove(info.Number, out info);

                _queueEvent.Set();
                return;
            }

            //if (Expressions.PlayerSay.IsMatch(e.Message))
            //{
            //    matches = e.Message.Split(new char[] { ' ' }, 3);
            //    ChatMessages.Add(new PlayerMessage(timestamp: DateTime.Now, player: matches[1], channel: matches[0], message: matches[2]));
            //    Logger.Debug("Player is " + matches[1] + " Channel is " + matches[0] + " Message is " + matches[2]);
            //}
        }

        private void ProgramLoop()
        {
            DateTime lastPlayerCheck = DateTime.Now;

            Logger.Info("------ Starting Whitelister ------");
            while (_running)
            {
                if (DateTime.Now >= lastPlayerCheck)
                {
                    //Logger.Debug("Sending \"Players\" BEPacket");
                    Client.SendCommand(BattlEyeCommand.Players);
                    lastPlayerCheck = DateTime.Now.AddSeconds(Config.Interval);
                }

                if (_queueEvent.WaitOne(5000))
                {
                    lock (_producerEventQueue)
                    {
                        while (_producerEventQueue.Count > 0)
                        {
                            var info = _producerEventQueue.Dequeue();
                            _consumerEventQueue.Enqueue(info);
                            CurrentPlayers[info.Player.Number] = info.Player;
                        }

                    }

                    Logger.Debug("Processing Queue at {0} Current Players", CurrentPlayers.Count);

                    lock (_consumerEventQueue)
                    {
                        while (_consumerEventQueue.Count > 0)
                        {
                            var args = _consumerEventQueue.Dequeue();
                            try
                            {
                                Logger.Debug("Handling Event For for {0}", args.Type);
                                RouteEvent(args);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Something bad happened :(");
                                Logger.Error("\t" + ex.Message);
                                Logger.Error("\t\t" + ex.StackTrace);
                            }
                        }
                    }
                }
            }

            if (_client.Connected)
                Client.Disconnect();

            if (Plugin != null)
                Plugin.TearDown();

            Logger.Info("------ Stopping Whitelister ------");
            Logger.Close();
        }

        public void RouteEvent(PlayerEventArgs e)
        {
            switch (e.Type)
            {
                case PlayerEventArgs.EventType.Connect:
                    OnPlayerConnectionEvent((PlayerConnectEventArgs)e);
                    return;
                case PlayerEventArgs.EventType.List:
                    OnPlayerListEvent((PlayerListEventArgs)e);
                    return;
                case PlayerEventArgs.EventType.Say:
                    OnPlayerSayEvent((PlayerSayEventArgs)e);
                    return;
                case PlayerEventArgs.EventType.Disconnect:
                    OnPlayerDisconnectEvent((PlayerDisconnectEventArgs)e);
                    return;
            }
        }

        public void Start()
        {
            if (!_running)
            {
                _running = true;
                ProgramLoop();
            }
        }

        public void Stop()
        {
            _running = false;
        }

        #region "Event Handlers"

        public event PlayerEventDelegate<PlayerConnectEventArgs> PlayerConnectionEvent;
        public event PlayerEventDelegate<PlayerDisconnectEventArgs> PlayerDisconnectEvent;
        public event PlayerEventDelegate<PlayerSayEventArgs> PlayerSayEvent;
        public event PlayerEventDelegate<PlayerListEventArgs> PlayerListEvent;


        private void OnPlayerConnectionEvent(PlayerConnectEventArgs args)
        {
            var handler = PlayerConnectionEvent;
            if (handler != null)
                handler(args);
        }

        private void OnPlayerDisconnectEvent(PlayerDisconnectEventArgs args)
        {

            var handler = PlayerDisconnectEvent;
            if (handler != null)
                handler(args);
        }

        private void OnPlayerSayEvent(PlayerSayEventArgs args)
        {
            var handler = PlayerSayEvent;
            if (handler != null)
                handler(args);
        }

        private void OnPlayerListEvent(PlayerListEventArgs args)
        {
            var handler = PlayerListEvent;
            if (handler != null)
                handler(args);
        }

        #endregion
    }
}
