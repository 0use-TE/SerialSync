using ElegantSeries;
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
	}
}
