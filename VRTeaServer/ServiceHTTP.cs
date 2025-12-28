using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class ServiceHTTP : IService
	{
		public ServiceHTTP()
		{
		}

		public async Task SessionClientAsync(int id, Session session, TcpClient client, CancellationTokenSource cts)
		{
			using (session)
			{
				NetworkStream stream = client.GetStream();
				try
				{
					await Task.WhenAny(
						Task.Run(async () =>
						{
							byte[] buffer = new byte[1024];
							while (true)
							{
								int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
								if (bytesRead == 0)
								{
									break;  // 相手が切断
								}
								session.RecvQueue.Enqueue(Encoding.UTF8.GetString(buffer, 0, bytesRead));
								session.Timestamp = DateTime.Now;
							}
							Console.WriteLine("disc read");
							cts.Cancel();
						}, cts.Token),
						Task.Run(async () =>
						{
							byte[]? sendBuffer = null;
							while (true)
							{
								try
								{

									while (session.SendQueue.TryDequeue(out sendBuffer))
									{
										await stream.WriteAsync(sendBuffer, cts.Token);
									}
									await Task.Delay(10, cts.Token);
								}
								catch (IOException)
								{
									Console.WriteLine("disc send");
									break;
								}
							}
						}, cts.Token));
				}
				catch (OperationCanceledException ex)
				{
					Console.WriteLine("disc cancell");
					_ = ex;
					return;
				}
				catch (IOException ex)
				{
					Console.WriteLine($"{ex}");
				}
				finally
				{
					cts.Cancel();
				}
			}
		}
	}
}
