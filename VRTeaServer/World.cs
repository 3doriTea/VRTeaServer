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
		public string Name { get; set; }
		public int Id { get; set; }
		public float PositionX { get; set; }
		public float PositionY { get; set; }
		public float PositionZ { get; set; }
		public float RotationX { get; set; }
		public float RotationY { get; set; }
		public float RotationZ { get; set; }
		public float ScaleX { get; set; }
		public float ScaleY { get; set; }
		public float ScaleZ { get; set; }
	}
	internal class World
	{
		public ConcurrentDictionary<int, Player> Players { get; set; } = new();
		public World()
		{
		}
	}
}
