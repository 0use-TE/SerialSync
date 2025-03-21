using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Models
{
    using Microsoft.Maui.Storage;
    using System.Diagnostics;

    public class LayoutInfo
    {
        public bool LogIsRight { get; set; }
        public bool UserSettingIsTop { get; set; }
        public bool SendIsTop { get; set; }
        public int FirstPane { get; set; } 
        public int SecondPanel { get; set; } 
        public int AcceptAndSend { get; set; } 
        public int PortAndUser { get; set; } 
        public int SendAndSended { get; set; }

        public static LayoutInfo Layout{ get; set; } = LoadLayoutInfo();
        // 保存布局信息到 Preferences
        public static void SaveLayoutInfo(LayoutInfo layoutInfo)
        {
            if (layoutInfo == null) return;
           // Debug.WriteLine(layoutInfo.FirstPane);

            Preferences.Set("LogIsRight", layoutInfo.LogIsRight);
            Preferences.Set("UserSettingIsTop", layoutInfo.UserSettingIsTop);
            Preferences.Set("SendIsTop", layoutInfo.SendIsTop);
            Preferences.Set("FirstPane", layoutInfo.FirstPane);
            Preferences.Set("SecondPanel", layoutInfo.SecondPanel);
            Preferences.Set("AcceptAndSend", layoutInfo.AcceptAndSend);
            Preferences.Set("PortAndUser", layoutInfo.PortAndUser);
            Preferences.Set("SendAndSended", layoutInfo.SendAndSended);
        }

        // 从 Preferences 加载布局信息
        public static LayoutInfo LoadLayoutInfo()
        {
            return new LayoutInfo
            {
                LogIsRight = Preferences.Get("LogIsRight", false),
                UserSettingIsTop = Preferences.Get("UserSettingIsTop", false),
                SendIsTop = Preferences.Get("SendIsTop", false),
                FirstPane = Preferences.Get("FirstPane", 20),
                SecondPanel = Preferences.Get("SecondPanel", 20),
                AcceptAndSend = Preferences.Get("AcceptAndSend", 50),
                PortAndUser = Preferences.Get("PortAndUser", 60),
                SendAndSended = Preferences.Get("SendAndSended", 10)
            };
        }


    }
}
