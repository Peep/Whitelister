using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Timers;

namespace BMRFME.Whitelist.Api
{
    public class ApiService : IApiService
    {
        public List<PlayerInformation> GetCurrentPlayers()
        {
            return Whitelister.Instance.CurrentPlayers.Select(k => k.Value).ToList();
        }

        public int GetPlayerCount()
        {
            return Whitelister.Instance.NumCurrentPlayers;
        }

        public string KickPlayer(string name, string reason)
        {
            var player = Whitelister.Instance.CurrentPlayers.Values.FirstOrDefault(r => r.Name == name);
            if (player == null)
                return "Player not found.";
            Whitelister.Instance.Plugin.KickPlayer(player, reason);
            return "Kick sent.";
        }

        public string BanPlayer(string name, string reason)
        {
            var player = Whitelister.Instance.CurrentPlayers.Values.FirstOrDefault(r => r.Name == name);
            if (player == null)
                return "Player not found.";
            Whitelister.Instance.Plugin.BanPlayer(player, reason);
            return "Ban sent.";
        }

        public string SendBroadcast(string message)
        {
            Whitelister.Instance.Client.SendCommand(BattleNET.BattlEyeCommand.Say, "-1 " + message);
            return "Broadcast sent.";
        }

        public string SendPrivateMessage(int playerNumber, string message)
        {
            Whitelister.Instance.Client.SendCommand(BattleNET.BattlEyeCommand.Say, playerNumber + " " + message);
            return "PM sent.";
        }

        public string RestartServer(int timeToRestart, string message)
        {
            int restartCountdown = timeToRestart;

            if (timeToRestart == 0)
                Whitelister.Instance.Client.SendCommand(BattleNET.BattlEyeCommand.Shutdown);

            var messageTimer = new Timer(timeToRestart / 10);
            messageTimer.Elapsed += (sender, args) =>
                SendBroadcast(String.Format("Restarting in {0} minutes. Reason: {1}",
                    ((restartCountdown / 1000) / 60), message));
            messageTimer.Enabled = true;

            var countdownTimer = new Timer(1000);
            countdownTimer.Elapsed += (sender, args) =>
            {
                if (restartCountdown > 0)
                    restartCountdown -= 1000;
                else
                    countdownTimer.Stop();
            };
            countdownTimer.Enabled = true;

            var restartTimer = new Timer(timeToRestart);
            restartTimer.Elapsed += (sender, args) =>
                    Whitelister.Instance.Client.SendCommand(BattleNET.BattlEyeCommand.Shutdown);
            restartTimer.Enabled = true;

            return "Restart sent.";
        }

        public string ReloadScripts()
        {
            Whitelister.Instance.Client.SendCommand(BattleNET.BattlEyeCommand.LoadScripts);
            return "Scripts reloaded.";
        }

        public List<PlayerMessage> ReceiveMessage()
        {
            return Whitelister.Instance.ChatMessages;
        }
    }
}
