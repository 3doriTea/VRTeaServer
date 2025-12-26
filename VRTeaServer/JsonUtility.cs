using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal static class JsonUtility
	{
		public static bool TryGet<T>(this JToken json, string key, out T? value)
		{
			object? prot = json[key];

			if (prot is null)
			{
				value = default;
				return false;
			}

			value = (T)prot;
			return true;
		}

		//public static bool TryGet<T>(this JObject json, string key, out T? value)
		//{
		//	object? prot = json[key];

		//	if (prot is null)
		//	{
		//		value = default;
		//		return false;
		//	}

		//	value = (T)prot;
		//	return true;
		//}
	}
}
