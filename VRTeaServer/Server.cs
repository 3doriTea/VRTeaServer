using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VRTeaServer
{

	class Session : IDisposable
	{
		public Session(TcpClient client)
		{
			Client = client;
		}

		public TcpClient Client { get; }
		public CancellationTokenSource cts { get; } = new();
		public ConcurrentQueue<byte[]> SendQueue { get; set; } = new();
		public ConcurrentQueue<string> RecvQueue { get; set; } = new();

		public void Dispose()
		{
			cts.Cancel();
			Client.Close();
			Client.Dispose();
		}
	}

	internal class Server : IDisposable
	{
		private ushort _port;
		internal readonly ConcurrentDictionary<int, Session> _sessions = [];
		private TcpListener? _listener;
		private readonly CancellationTokenSource _cts = new();

		public Server(ushort port)
		{
			_port = port;
		}

		public void Dispose()
		{
			foreach (var (id, session) in _sessions)
			{
				session.Dispose();
			}
		}

		public async Task Start()
		{
			IPEndPoint localIPEP = new(IPAddress.Any, _port);
			_listener = new(localIPEP);

			_listener.Start();

			while (true)
			{
				try
				{
					TcpClient tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
					Session session = _sessions.AddOrUpdate(
						_sessions.Count,
						new Session(tcpClient),
						(int id, Session s) =>
						{
							s.Dispose();
							return new Session(tcpClient);
						});
					_ = SessionClientAsync(session, tcpClient, session.cts);
				}
				catch (OperationCanceledException ex)
				{
					break;
				}
			}
		}

		private static async Task SessionClientAsync(Session session, TcpClient client, CancellationTokenSource cts)
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
							int bytesRead = 0;
							while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
							{
								session.RecvQueue.Enqueue(Encoding.UTF8.GetString(buffer));
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

		public void Stop()
		{
			_cts.Cancel();
		}
	}
}
