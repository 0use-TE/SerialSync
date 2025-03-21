using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Models
{
    using Microsoft.Maui.Storage;

    public class LayoutInfo
    {
        public bool LogIsRight { get; set; }
        public bool UserSettingIsTop { get; set; }
        public bool SendIsTop { get; set; }
        public int FirstPane { get; set; } = 20;
        public int SecondPanel { get; set; } = 20;
        public int AcceptAndSend { get; set; } = 40;
        public int PortAndUser { get; set; } = 60;
        public int SendAndSended { get; set; } = 15;

      
    }
}
