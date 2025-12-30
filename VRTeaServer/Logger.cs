using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class Logger
	{
		public class Log
		{
			public string Content{ get; set; }
			public DateTime Timestapm{ get; set; }

			public Log(string content, DateTime timestamp)
			{
				Content = content;
				Timestapm = timestamp;
			}

			public override string ToString()
			{
				return $"{Timestapm:HH:mm:ss%K}";
			}
		}

		public Logger() { }

		public void WriteLine(string content)
		{
			string log = $"[{}]{content}";
			Console.WriteLine();
		}
	}
}
