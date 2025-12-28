using System.Net;
using VRTeaServer;

class Program
{
	public static void Main(string[] args)
	{
		Console.WriteLine("World openning...");
		World world = new();
		Console.Write("Ok!");

		Console.WriteLine("Server booting...");

		string? ipAddr = null;
		while (string.IsNullOrEmpty(ipAddr))
		{
			Console.WriteLine("IPAddress:");
			ipAddr = Console.ReadLine();

			if (string.IsNullOrEmpty(ipAddr))
			{
				Console.WriteLine("Is to use IPAddress.Any? It is allow all access. (Y/n):");
				if (Console.ReadLine() == "Y")
				{
					ipAddr = "";
					break;
				}
			}
		}

		Server server = new(3333, ipAddr);
		var serverTask = server.Start();
		Console.Write("Ok!");

		Console.WriteLine("God ready...");
		God god = new(world, server);
		CancellationTokenSource cts = new();
		var godTask = god.Start(cts.Token);
		Console.Write("Ok!");

		Task.WhenAll(
			serverTask,
			godTask,
			Task.Run(async () =>
			{
				Console.ReadLine();
				cts.Cancel();
				server.Stop();
			})).Wait();

		Console.WriteLine("Shutdown...");
		Console.Write("Ok!");
	}
}
