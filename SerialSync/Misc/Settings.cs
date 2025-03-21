using ElegantSeries;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Misc
{
	public static class Settings
	{
		public static DialogOptions DialogOptions { get; set; } = new DialogOptions
		{
			FullWidth = true,
			CloseButton = true,
			NoHeader = true
		};
        public  static MauiSerialPort MauiSerialPort { get; set; } = new MauiSerialPort();
        public static List<string> SendedMsg { get; set; } = new List<string>();
        public  static List<string> ReceivedMsg { get; set; } = new List<string>();
		public static string LogPath { get; set; }=Path.Combine(FileSystem.AppDataDirectory, "SerialSync", "log-.txt");
    }
}
