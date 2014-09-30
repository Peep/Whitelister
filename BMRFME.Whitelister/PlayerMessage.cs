using System;
using System.Runtime.Serialization;

namespace BMRFME.Whitelist
{
    [DataContract]
    public class PlayerMessage
    {
        [DataMember]
        public DateTime Timestamp;
        [DataMember]
        public string Player;
        [DataMember]
        public string Channel;
        [DataMember]
        public string Message;

        public PlayerMessage(DateTime timestamp, string player, string channel, string message)
        {
            Timestamp = timestamp;
            Player = player;
            Channel = channel;
            Message = message;
        }
    }
}
