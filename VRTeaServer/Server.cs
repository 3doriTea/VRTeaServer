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
	readonly struct RecvData
	{
		public readonly byte[] buffer;

		public RecvData(byte[] bytes)
		{
			buffer = bytes;
		}

		public readonly string GetString()
		{
			return Encoding.UTF8.GetString(buffer);
		}
	}
	class Session : IDisposable
	{
		public Session(TcpClient client, int id)
		{
			Client = client;
			Id = id;
			Timestamp = DateTime.Now;
		}

		public int Id { get; }

		public TcpClient Client { get; }
		public bool HasDeathOmen { get; set; } = false;
		public CancellationTokenSource cts { get; } = new();
		public ConcurrentQueue<byte[]> SendQueue { get; set; } = new();
		public ConcurrentQueue<RecvData> RecvQueue { get; set; } = new();

		// 最後に通信した時刻
		public DateTime Timestamp{ get; set; }

		public void Dispose()
		{
			cts.Cancel();
			Client.Close();
			Client.Dispose();
		}
	}

	internal class Server : IDisposable
	{
		private ushort _portGame;
		private ushort _portHTTP;
		private string _address;
		internal readonly ConcurrentDictionary<int, Session> _sessions = [];
		private TcpListener? _listenerHTTP;
		private TcpListener? _listenerGame;
		private readonly CancellationTokenSource _cts = new();

		public Action<int> OnDisconnected { get; set; } = delegate { };


		public Server(ushort portGame, ushort portHTTP, string address)
		{
			_portGame = portGame;
			_portHTTP = portHTTP;
			_address = address;

			OnDisconnected += Disconnect;
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
			IPEndPoint? localIPEPGame;
			IPEndPoint? localIPEPHTTP;
			if (string.IsNullOrEmpty(_address))
			{
				localIPEPGame = new(IPAddress.Any, _portGame);
				localIPEPHTTP = new(IPAddress.Any, _portHTTP);
			}
			else
			{
				localIPEPGame = new(IPAddress.Parse(_address), _portGame);
				localIPEPHTTP = new(IPAddress.Parse(_address), _portHTTP);
			}

			if (localIPEPGame.Equals(localIPEPHTTP))
			{
				_listenerGame = new (localIPEPGame);
				_listenerHTTP = _listenerGame;
			}
			else
			{
				_listenerGame = new (localIPEPGame);
				_listenerHTTP = new (localIPEPHTTP);
			}

			_listenerGame.Start();
			_listenerHTTP.Start();

			int sessionIdCounter = 0;

			async Task AcceptProcAsync(TcpListener listener, IService service)
			{
				while (true)
				{
					try
					{
						TcpClient tcpClient = await listener.AcceptTcpClientAsync(_cts.Token);
						
						// キープアライブの設定
						tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
						tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 5);
						tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 1);
						tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);

						Session session = _sessions.AddOrUpdate(
							sessionIdCounter,
							new Session(tcpClient, sessionIdCounter),
							(int id, Session s) =>
							{
								s.Dispose();
								return new Session(tcpClient, sessionIdCounter);
							});
						_ = service.SessionClientAsync(sessionIdCounter, session, tcpClient, session.cts);
					}
					catch (OperationCanceledException ex)
					{
						_ = ex;
						break;
					}
					sessionIdCounter++;
				}
			};

			await Task.WhenAll(
			[
				AcceptProcAsync(_listenerGame, new ServiceGame(this)),
				AcceptProcAsync(_listenerHTTP, new ServiceHTTP())
			]);
		}

		private void Disconnect(int id)
		{
			_sessions.Remove(id, out _);
		}

		public void Stop()
		{
			_cts.Cancel();
		}
	}
}
