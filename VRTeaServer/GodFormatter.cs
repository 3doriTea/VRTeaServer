using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class GodFormatter
	{
		public string Method { get; set; } = "";
		public string Directory { get; set; } = "";
		public string Version { get; set; } = "";

		public static GodFormatter Load(string request)
		{
			string[] parts = request.Split(' ');
			return new GodFormatter{ Method = parts[0], Directory = parts[1], Version = parts[2] };
		}
	}
	internal class HttpResponce
	{
		public string Version { get; set; } = "HTTP/1.1";
		public uint StatusCode { get; set; } = 200;
		public string Status { get; set; } = "OK";
		public string ContentType { get; set; } = "text/html; charset=UTF-8";
		public string Content { get; set; } = "";

		public void StoreResponce(out string text)
		{
			text = string.Join("\n",
			[
				$"{Version} {StatusCode} {Status}",
				$"Content-Type: {ContentType}",
				$"Content-Length: {Encoding.UTF8.GetByteCount(Content)}",
				"",
				$"{Content}"
			]);
		}
	}

}
