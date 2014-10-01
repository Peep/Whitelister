using EasyConfigLib.Storage;

namespace BMRFME.Whitelist
{
    public class ConfigWhitelist
        : EasyConfig
    {

        [Field("Port", "BEye")]
        public int BattlEyePort = 3202;

        [Field("Address")]
        public string BattlEyeAddr = "127.0.0.1";

        [Field("Pass")]
        public string BattlEyePass = "?";

        [Field("Scan Interval")]
        public int Interval = 30000;

        [Field("", "Messages")]
        public string KickMessage = "You have been kicked because you are not authorized";

        [Field("Log To Console")]
        public bool LogToConsole;

        [Field("Rate Limit")]
        public int RateLimitSeconds = 30;

        [Field("PlayerConnectionStageOne", "RegularExpressions")]
        public string PlayerConnectionStageOneRegex = @"^Player[\s]#?([0-9]+)#?[\s](.+)[\s]\((.+):(.+)\)[\s]connected";

        [Field("PlayerConnectionStageTwo")]
        public string PlayerConnectionStageTwoRegex = @"^Player[\s]#?([0-9]+)#?[\s](.+)[\s]-[\s]Guid:[\s]([\w]+)[\s]\(";

        [Field("PlayerDisconnect")]
        public string PlayerDisconnectRegex = @"^Player\s#([0-9]+)\s(.+)\sdisconnected";

        [Field("PlayerListHeader")]
        public string PlayerListHeaderRegex = @"^Players[\s]on[\s]server";

        [Field("PlayerListValue")]
        public string PlayerListValueRegex = @"([0-9]+)[\s]+(.+):(.+)[\s]+(-?[0-9]+)[\s]+([\w]+)\(OK\)[\s]+(.+)";

        [Field("PlayerName")]
        public string PlayerNameRegex = @"^(.+)(\s\(Lobby\))+";

        [Field("PlayerSay")]
        public string PlayerSayRegex = @"\((Global|Side|Direct|Vehicle)\)\040.*:\040.*";

        [Field("", "Plugin")]
        public string Plugin = "None";


        public ConfigWhitelist(string file)
            : base(file)
        {
        }
    }
}
