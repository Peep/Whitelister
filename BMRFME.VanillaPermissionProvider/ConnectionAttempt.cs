using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMRFME.VBulletinPermissionProvider
{
    class ConnectionAttempt
    {
        public int Attempts { get; set; }
        public DateTime FirstAttempt { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}
