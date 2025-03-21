using Microsoft.UI.Xaml.Documents;
using SerialSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Misc
{

	public static class Unilities
	{
        // 保存布局信息到 Preferences
        public static void SaveLayoutInfo(LayoutInfo layoutInfo)
        {
            if (layoutInfo == null) return;

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
                AcceptAndSend = Preferences.Get("AcceptAndSend", 40),
                PortAndUser = Preferences.Get("PortAndUser", 60),
                SendAndSended = Preferences.Get("SendAndSended", 15)
            };
        }
    }
}
