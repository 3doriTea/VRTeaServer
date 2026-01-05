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
				return $"[{Timestapm:HH:mm:ss%K}] {Content}";
			}
		}


		private readonly string _logFilePath;
		private readonly List<Log> _logs = [];
		public Logger(string logFilePath)
		{
			_logFilePath = logFilePath;
		}

		public void WriteLine(string content)
		{
			_logs.Add(new Log(content, DateTime.Now));
			Console.WriteLine(_logs.Last());
		}

		/// <summary>
		/// ログをファイルに書き出す
		/// </summary>
		/// <returns>非同期処理タスク</returns>
		public async Task WriteOutLog()
		{
			string? logsText;
			lock (_logs)
			{
				logsText = string.Join("\n", _logs);
				_logs.Clear();
			}
			await File.WriteAllTextAsync(_logFilePath, logsText);
		}
	}
}
