using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer.Web
{
	internal class HttpResponce
	{
		public string Version { get; set; } = "HTTP/1.1";
		public uint StatusCode { get; set; } = 200;
		public string Status { get; set; } = "OK";
		public string ContentType { get; set; } = "text/html; charset=UTF-8";
		public byte[] Content{ get; set; } = [];

		public void SetContent(string content)
		{
			Content = Encoding.UTF8.GetBytes(content);
		}
		public void SetContentTypeByExtension(string extension)
		{
			ContentType = extension.ToLower() switch
			{
				".html" => "text/html; charset=UTF-8",
				".css" => "text/css",
				".png" => "image/png",
				".jpg" => "image/jpeg",
				".jpeg" => "image/jpeg",
				".json" => "application/json; charset=UTF-8",
				_ => "application/octet-stream"
			};
		}
		public void StoreResponce(out byte[] buffer)
		{
			var header = string.Join("\r\n",
			[
				$"{Version} {StatusCode} {Status}",
				$"Content-Type: {ContentType}",
				$"Content-Length: {Content.Length}",
				"",
				"",
			]);
			int headerSize = Encoding.UTF8.GetByteCount(header);
			buffer = new byte[headerSize + Content.Length];
			Encoding.UTF8.GetBytes(header).AsSpan().CopyTo(buffer.AsSpan());
			Content.AsSpan().CopyTo(buffer.AsSpan(headerSize));
		}
	}
}
