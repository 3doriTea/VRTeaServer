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
		public CancellationToken cancellationToken { get; } = new();
		public ConcurrentQueue<string> SendQueue { get; set; } = new();
		public ConcurrentQueue<string> RecvQueue { get; set; } = new();

		public void Dispose()
		{
			cancellationToken.ThrowIfCancellationRequested();
			Client.Close();
			Client.Dispose();
		}
	}

	internal class Server : IDisposable
	{
		private ushort _port;
		private readonly ConcurrentDictionary<int, Session> _sessions = [];
		private TcpListener? _listener;
		private readonly CancellationToken _cancellationToken = new();

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

		public async void Start()
		{
			IPEndPoint localIPEP = new(IPAddress.Any, _port);
			_listener = new(localIPEP);

			_listener.Start();

			while (true)
			{
				try
				{
					TcpClient tcpClient = await _listener.AcceptTcpClientAsync(_cancellationToken);
					Session session = _sessions.AddOrUpdate(
						_sessions.Count,
						new Session(tcpClient),
						(int id, Session s) =>
						{
							s.Dispose();
							return new Session(tcpClient);
						});
					_ = SessionClientAsync(session, tcpClient, session.cancellationToken);
				}
				catch (OperationCanceledException ex)
				{
					break;
				}
			}
		}

		private async Task SessionClientAsync(Session session, TcpClient client, CancellationToken cancellationToken)
		{
			using (client)
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[1024];

				try
				{
					int bytesRead = 0;
					while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
					{
						session.RecvQueue.Enqueue(new RecvData())
						buffer
					}
				}
				catch (OperationCanceledException ex)
				{
					session.Dispose();
				}
			}
		}

		public void Stop()
		{
			_cancellationToken.ThrowIfCancellationRequested();
		}
	}
}
