using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VRTeaServer
{
	internal static class NameGenerator
	{
		static readonly string[] NAME_KEY_WORDS =
		[
			"",
			"みゅ",
			"こす",
			"こり",
			"みゃ",
			"みゅ",
			"ふぁ",
			"ふぃ",
			"ふ",
			"ふぉ",
			"にゃ",
			"にゅ",
			"にょ",
			"ん",
			"の",
			"ぬ",
		];

		public static string Generate(int id)
		{
			byte[] bytes = Encoding.UTF8.GetBytes($"{id}{id}{int.MaxValue - id}{id}{id}{int.MaxValue - id}");

			byte[] hashBytes = SHA256.HashData(bytes);

			string hash = Convert.ToHexString(hashBytes).ToLower();
			string[] buffer = new string[8];
			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = NAME_KEY_WORDS[int.Parse($"{hash[i]}", NumberStyles.HexNumber)];
			}
			return string.Join("", buffer);
		}
	}
}
