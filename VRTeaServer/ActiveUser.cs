using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal enum ActiveUserStatus
	{
		Free,
		TryLogin,
		Logined,
	}

	internal class ActiveUser
	{
		

		public ActiveUserStatus Status { get; set; } = ActiveUserStatus.Free;
		public string Name { get; set; } = "";
	}
}
