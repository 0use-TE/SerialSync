using ElegantSeries;
using SerialSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Services
{
	public class GlobalState
	{
		public MauiSerialPort MauiSerialPort { get; set; } = new MauiSerialPort();
		public List<string> SendedMsg { get; set; } = new List<string>();
        public List<string> ReceivedMsg { get; set; } = new List<string>();
		public LayoutInfo LayoutInfo { get; set; } = new LayoutInfo();	

    }
}
