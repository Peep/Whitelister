using System;
using System.Runtime.Serialization;

namespace BMRFME.Whitelist
{
    [DataContract]
    public class PlayerInformation
    {
        public PlayerInformation(string name, string ipaddr, string port, string number)
        {
            Name = name.Contains(" (Lobby)") ? name.Substring(0, name.LastIndexOf(" (Lobby)", System.StringComparison.Ordinal)) : name;
            IpAddr = ipaddr;
            Port = int.Parse(port);
            Number = int.Parse(number);
        }

        [DataMember]
        public string Guid;
        [DataMember]
        public string Name;
        [DataMember]
        public readonly string IpAddr;
        [DataMember]
        public readonly int Port;
        [DataMember]
        public readonly int Number;
    }
}