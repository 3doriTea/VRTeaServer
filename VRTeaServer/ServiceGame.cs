using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class ServiceGame : IService
	{
		private readonly Server server;

		public ServiceGame(Server server)
		{
			this.server = server;
		}

		public async Task SessionClientAsync(int id, Session session, TcpClient client, CancellationTokenSource cts)
		{
			using (session)
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[1024];

				try
				{
					await Task.WhenAll(
						Task.Run(async () =>
						{
							while (true)
							{
								int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
								if (bytesRead == 0)
								{
									// 通信切断のフラグを立てる
									session.HasDeathOmen = true;
									break;
								}
								session.RecvQueue.Enqueue(Encoding.UTF8.GetString(buffer, 0, bytesRead));
								session.Timestamp = DateTime.Now;
							}
						}, cts.Token),
						Task.Run(async () =>
						{
							byte[]? sendBuffer = null;
							while (true)
							{
								while (session.SendQueue.TryDequeue(out sendBuffer))
								{
									await stream.WriteAsync(sendBuffer, cts.Token);
								}
								await Task.Delay(10, cts.Token);
							}
						}, cts.Token));
				}
				catch (OperationCanceledException ex)
				{
					_ = ex;
					return;
				}
				catch (IOException ex)
				{
					Console.WriteLine($"{ex}");
				}
			}
		}
	}
}
