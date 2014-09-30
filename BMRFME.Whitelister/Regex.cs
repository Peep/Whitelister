using System;
using System.Text.RegularExpressions;

namespace BMRFME.Whitelist
{
    public class RegExp
    {
        public readonly Regex PlayerConnectStg1;
        public readonly Regex PlayerConnectStg2;
        public readonly Regex PlayerDisconnect;
        public readonly Regex PlayerListHeader;
        public readonly Regex PlayerListValue;
        public readonly Regex PlayerName;
        public readonly Regex PlayerSay;

        public RegExp(ConfigWhitelist config)
        {
            try
            {
                PlayerConnectStg1 = new Regex(config.PlayerConnectionStageOneRegex,
                                              RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline
                                              | RegexOptions.Compiled);

                PlayerConnectStg2 = new Regex(config.PlayerConnectionStageTwoRegex,
                                              RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline
                                              | RegexOptions.Compiled);
                PlayerListValue = new Regex(config.PlayerListValueRegex,
                                            RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline
                                            | RegexOptions.Compiled);

                PlayerDisconnect = new Regex(config.PlayerDisconnectRegex,
                                             RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline
                                             | RegexOptions.Compiled);

                PlayerListHeader = new Regex(config.PlayerListHeaderRegex,
                                             RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline
                                             | RegexOptions.Compiled);

                PlayerName = new Regex(config.PlayerNameRegex,
                                       RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Compiled);

                PlayerSay = new Regex(config.PlayerSayRegex,
                                      RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch (Exception)
            {
                if (PlayerConnectStg1 == null)
                    throw new Exception("PlayerConnectStageOne Bad Regex");

                if (PlayerConnectStg2 == null)
                    throw new Exception("PlayerConnectStg2 Bad Regex");

                if (PlayerListValue == null)
                    throw new Exception("PlayerListValue Bad Regex");

                if (PlayerDisconnect == null)
                    throw new Exception("PlayerDisconnect Bad Regex");

                if (PlayerListHeader == null)
                    throw new Exception("PlayerListHeader Bad Regex");

                if (PlayerName == null)
                    throw new Exception("PlayerName Bad Regex");

                if (PlayerSay == null)
                    throw new Exception("PlayerSay Bad Regex");
            }
        }
    }
}
