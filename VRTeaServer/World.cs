using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VRTeaServer
{
	class Player
	{
		public int Id { get; set; } = 0;
		public string Name { get; set; } = string.Empty;
		public float PositionX { get; set; } = 0.0f;
		public float PositionY { get; set; } = 0.0f;
		public float PositionZ { get; set; } = 0.0f;
		public int PositionW { get; set; } = 0;
		public float RotationY { get; set; } = 0.0f;
	}
	internal class World
	{
		public ConcurrentDictionary<string, int> NameToId { get; set; } = new();
		public ConcurrentDictionary<int, Player> Players { get; set; } = new();
		public World()
		{
		}
	}
}
