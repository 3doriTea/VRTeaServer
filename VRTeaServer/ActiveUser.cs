using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class ActiveUser
	{
		public enum Status
		{
			Free,
			TryLogin,
			Logined,
		}

		public Status status { get; set; } = Status.Free;
	}
}
