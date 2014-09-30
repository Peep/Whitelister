namespace BMRFME.Whitelist.Events
{
    public class PlayerListEventArgs
        : PlayerEventArgs
    {

        public PlayerListEventArgs(PlayerInformation info)
            : base(EventType.List, info)
        {
        }
    }
}
