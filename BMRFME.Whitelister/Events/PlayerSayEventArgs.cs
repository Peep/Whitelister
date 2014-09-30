namespace BMRFME.Whitelist.Events
{
    public class PlayerSayEventArgs
        : PlayerEventArgs
    {
        public string Channel;
        public string Message;

        public PlayerSayEventArgs(PlayerInformation info, string channel, string message)
            : base(EventType.Say, info)
        {
            Channel = channel;
            Message = message;
        }
    }
}
