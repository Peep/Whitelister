using System;
using System.Collections.Generic;
using System.Linq;

namespace BMRFME.VBulletinPermissionProvider
{
    public class ConnectionAttempts
    {
        private List<ConnectionAttempt> _connAttempts = new List<ConnectionAttempt>();

        public int Within(string ipAddr, int seconds)
        {
            return _connAttempts.Where(conAttempt => conAttempt.IpAddress == ipAddr)
                .Count(conAttempt => conAttempt.Time <= DateTime.Now.AddSeconds(seconds) 
                    && conAttempt.Time >= DateTime.Now.AddSeconds(-seconds));
        }

        public void Add(string ipAddr)
        {
            _connAttempts.Add(new ConnectionAttempt(ipAddr));
        }
    }

    class ConnectionAttempt
    {
        public string IpAddress { get; private set; }
        public DateTime Time { get; private set; }

        public ConnectionAttempt(string ipAddr)
        {
            IpAddress = ipAddr;
            Time = DateTime.Now;
        }
    }
}
