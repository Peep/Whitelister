namespace BMRFME.Whitelist.Events
{
    public class PlayerConnectEventArgs
        : PlayerEventArgs
    {

        public PlayerConnectEventArgs(PlayerInformation info)
            : base(EventType.Connect, info)
        {

        }
    }
}