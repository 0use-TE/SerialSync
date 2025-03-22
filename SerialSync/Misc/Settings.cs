using MudBlazor;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Misc
{
	public static class Settings
	{
		public static DialogOptions DialogOptions { get; private set; } = new DialogOptions
		{
			FullWidth = true,
			CloseButton = true,
			NoHeader = true
		};
        public  static SerialPort SerialPort { get;private set; } = new SerialPort();
        public static List<SendModel> SendedMsg { get; set; } = new List<SendModel>();
        public  static List<ReciveModel> ReceivedMsg { get; set; } = new List<ReciveModel>();
		public static string LogPath { get; set; }=Path.Combine(FileSystem.AppDataDirectory, "SerialSync", "log-.txt");
    }
	public class SendModel
	{
		public string ?SendMsg { get; set; }
		public TimeSpan Span { get; set; }
	}
    public class ReciveModel
    {
        public string? RecivedMsg { get; set; }
        public TimeSpan Span { get; set; }
    }
}
