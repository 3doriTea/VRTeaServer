using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class Logger
	{
		public struct Log
		{
			public string Content{ get; set; }
			public DateTime Timestapm{ get; set; }
			public char Prefix{ get; set; }

			public Log(char prefix, string content, DateTime timestamp)
			{
				Prefix = prefix;
				Content = content;
				Timestapm = timestamp;
			}

			public override string ToString()
			{
				return $"{Prefix}:[{Timestapm:HH:mm:ss%K}] {Content}";
			}
		}

		public float SaveIntervalSec { get; set; } = 10;
		private readonly string _logFileDirPath;
		private readonly List<Log> _logs = [];
		public Logger(string logFileDirPath)
		{
			_logFileDirPath = logFileDirPath;
		}

		public async Task Start(CancellationTokenSource cts)
		{
			try
			{
				while(true)
				{
					await Task.Delay((int)(SaveIntervalSec * 1000), cts.Token);
					await WriteOutLog();
				}
			}
			catch (TaskCanceledException ex)
			{
				return;
			}
		}

		public void WriteLine(string content)
		{
			var log = new Log('>', content, DateTime.Now);
			lock (_logs)
			{
				_logs.Add(log);
			}
			Console.WriteLine(log);
		}

		public void Write(string content)
		{
			bool useLine = false;
			lock (_logs)
			{
				useLine = _logs.Count == 0 || _logs.Last().Content[^content.Length..] != content;
			}
			if (useLine)
			{
				WriteLine(content);
				return;
			}
			lock (_logs)
			{
				Log log = new Log('>', _logs.Last().Content + content, DateTime.Now);
				_logs[^1] = log;
			}
			Console.Write(content);
		}

		public void Error(string content)
		{
			Log log = new Log('E', content, DateTime.Now);
			lock (_logs)
			{
				_logs.Add(log);
			}
			Console.WriteLine(log);
		}

		/// <summary>
		/// ログをファイルに書き出す
		/// </summary>
		/// <returns>非同期処理タスク</returns>
		public async Task WriteOutLog()
		{
			string? logsText;
			int count = 0;
			lock (_logs)
			{
				logsText = string.Join("\n", _logs);
				count = _logs.Count;
				_logs.Clear();
			}
			try
			{
				await File.WriteAllTextAsync($"{_logFileDirPath}/{DateTime.Now:HH-mm-ss}_{count:000}.log", logsText);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}
