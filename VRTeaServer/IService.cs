using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal interface IService
	{
		Task SessionClientAsync(int id, Session session, TcpClient client, CancellationTokenSource cts);
	}
}
