using System.Net;
using VRTeaServer;

class Program
{
	const ushort PortGameService = 3333;
	const ushort PortHTTPService = 80;
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

		Console.WriteLine("Do you want to use port 3333 for both services, or separate them (Web: 80, Game: 3333)? (Y/n):");
		string? isUseBoth = Console.ReadLine();

		Server? server = null;
		if (isUseBoth == "Y")
		{
			server = new(PortGameService, PortGameService, ipAddr);
		}
		else
		{
			server = new(PortGameService, PortHTTPService, ipAddr);
		}


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
