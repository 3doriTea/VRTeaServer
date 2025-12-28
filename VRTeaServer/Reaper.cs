using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	/// <summary>
	/// 通信の死神
	/// </summary>
	internal class Reaper
	{
		public float IntervalSec { get; set; }
		private Server server;
		public Reaper(Server server, float intervalSec)
		{
			this.server = server;
			IntervalSec = intervalSec;
		}

		public async Task Start(CancellationTokenSource cts)
		{
			try
			{
				while (true)
				{
					var toRemoveIds = new List<int>();

					var now = DateTime.Now;
					await Task.Delay((int)(IntervalSec * 1000), cts.Token);
					foreach(var (sessionId, session) in server._sessions)
					{
						TimeSpan diff = now - session.Timestamp;
						if (diff.TotalSeconds > IntervalSec)
						{
							// 切断フラグを立てる
							session.HasDeathOmen = true;
						}

						// 削除予定リストに追加
						if (session.HasDeathOmen)
						{
							server.OnDisconnected(sessionId);
							toRemoveIds.Add(sessionId);
						}
					}

					// 実際に削除
					foreach (var id in toRemoveIds)
					{
						server._sessions.Remove(id, out _);
					}
				}
			}
			catch(OperationCanceledException)
			{
				return;
			}
		}
	}
}
