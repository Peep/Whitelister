using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using BMRFME.Whitelist;
using NetFwTypeLib;

namespace BMRFME.VanillaPermissionProvider
{
    public class Firewall
    {
        private static HashSet<string> _rules = new HashSet<string>();

        const string INetFwPolicy2ProgID = "HNetCfg.FwPolicy2";
        const string INetFwRuleProgID = "HNetCfg.FWRule";

        public static void AddBan(string ipAddr, string ruleName, int durationSeconds = 0)
        {
            var rule = GetComObject<INetFwRule2>(INetFwRuleProgID);
            var policy = GetComObject<INetFwPolicy2>(INetFwPolicy2ProgID);

            rule.Name = ruleName;
            rule.Description = String.Format(
                "Block inbound traffic over UDP ports used by Arma due to too many join attempts." +
                    "This rule should only exist temporarily.");

            rule.RemoteAddresses = ipAddr;
            rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
            //rule.LocalPorts = "2102,2104,2105,2202,2204,2205,8761,8762,27011,27012"
            // + "," + Whitelister.Instance.Config.BattlEyePort;
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.Profiles = policy.CurrentProfileTypes;
            rule.Grouping = "@firewallapi.dll, -23255";
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            rule.InterfaceTypes = "All";
            rule.Enabled = true;

            policy.Rules.Add(rule);
            _rules.Add(ruleName);

            if (durationSeconds != 0)
            {
                var timer = new Timer(durationSeconds * 1000);
                timer.Elapsed += (sender, args) =>
                {
                    RemoveBan(ruleName);
                    Whitelister.Instance.Logger.Info("Removing Firewall Rule: " + ruleName);
                    timer.Stop();
                };
                timer.Enabled = true;
            }
        }

        public static void RemoveBan(string ruleName)
        {
            var policy = GetComObject<INetFwPolicy2>(INetFwPolicy2ProgID);
            policy.Rules.Remove(ruleName);
            _rules.Remove(ruleName);
        }

        public static void RemoveAllBans()
        {
            foreach (var rule in _rules.ToList())
                RemoveBan(rule);
        }

        private static T GetComObject<T>(string progID)
        {
            Type t = Type.GetTypeFromProgID(progID, true);
            return (T)Activator.CreateInstance(t);
        }
    }
}
