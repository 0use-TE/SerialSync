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
	}
}
