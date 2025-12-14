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
		Server server = new(33333);
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
