namespace BMRFME.Whitelist.Events
{
    public class PlayerDisconnectEventArgs
        : PlayerEventArgs
    {
        public PlayerDisconnectEventArgs(PlayerInformation info)
            : base(EventType.Disconnect, info)
        {
        }
    }
}
