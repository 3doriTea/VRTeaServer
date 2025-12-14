using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class God
	{
		private int intervalMillSec = 10;
		private World _world;
		private Server _server;

		public God(World world, Server server)
		{
			_world = world;
			_server = server;
		}

		public async Task Start(CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					//_world.
					foreach (var (id, session) in _server._sessions)
					{
						string? request;
						if (session.RecvQueue.TryDequeue(out request))
						{
							var format = GodFormatter.Load(request);
							Console.WriteLine($"format.Method={format.Method}, format.Directory={format.Directory}, format.Version={format.Version}");
							Console.WriteLine(new string('-', 30));

							var responce = new HttpResponce
							{
								Content = string.Join("\n",
								[
									"<!DOCTYPE html>",
									"<html lang=\"ja\">",
									"<head>",
									"",
									"	<meta charset=\"UTF-8\">",
									"",
									"	<title>ページタイトル</title>",
									"</head>",
									"<body>",
									"",
									"	<h1>Hello World!</h1>",
									"",
									"	<p>内容がないよー</p>",
									"</body>",
									"</html>",
								])
							};

							responce.StoreResponce(out var str);
							session.SendQueue.Enqueue(str);
						}
					}

					Console.Write(".");

					await Task.Delay(intervalMillSec, cancellationToken);
				}
			}
			catch (TaskCanceledException ex)
			{
				return;
			}
		}
	}
}
