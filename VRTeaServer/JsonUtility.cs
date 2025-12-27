using Newtonsoft.Json.Linq;

namespace VRTeaServer
{
	internal static class JsonUtility
	{
		public static bool TryGet<T>(this JToken? json, string key, out T? value)
		{
			value = default;

			var j = json?[key];
			if (j is null)
			{
				return false;
			}

			try
			{
				if (j.Value<T?>() is T v)
				{
					value = v;
					return true;
				}
			}
			catch
			{
			}
			return false;
		}
		public static bool TryGet<T>(this JObject json, string key, out T? value)
		{
			value = default;

			try
			{
				if (json.TryGetValue(key, out var obj))
				{
					value = obj.Value<T>();
					return true;
				}
				else
				{
					return false;
				}
			}
			catch
			{
			}
			return false;
		}
	}
}
