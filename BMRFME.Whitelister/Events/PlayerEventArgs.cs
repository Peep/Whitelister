using System;

namespace BMRFME.Whitelist.Events
{
    public delegate void PlayerEventDelegate<in T>(T args) where T : PlayerEventArgs;

    public class PlayerEventArgs
        : EventArgs
    {
        #region EventType enum

        public enum EventType
        {
            Connect,
            List,
            Disconnect,
            Say
        }

        #endregion

        public readonly EventType Type;
        public readonly PlayerInformation Player;

        protected PlayerEventArgs(EventType type, PlayerInformation info)
        {
            Type = type;
            Player = info;
        }
    }
}
