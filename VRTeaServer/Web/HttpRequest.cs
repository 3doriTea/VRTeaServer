using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer.Web
{
	internal class HttpRequest
	{
		public string Method { get; set; } = "";
		public string Directory { get; set; } = "";
		public string Version { get; set; } = "";
		public string Content { get; set; } = "";

		public static HttpRequest Load(string request)
		{
			string[] parts = request.Split(' ');
			int contentBegin = request.IndexOf("\r\n\r\n") + 4;
			return new HttpRequest{ Method = parts[0], Directory = parts[1], Version = parts[2], Content = request[contentBegin..] };
		}
	}
}
