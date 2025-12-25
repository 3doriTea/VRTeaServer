using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using VRTeaServer.DB;
using VRTeaServer.Web;

namespace VRTeaServer
{
	internal class God
	{
		private int intervalMillSec = 10;
		private World _world;
		private Server _server;
		private ConcurrentDictionary<string, ActiveUser> _activeUsers = [];
		private DBConnector _dBConnector = new();

		public God(World world, Server server)
		{
			_world = world;
			_server = server;
			_dBConnector.InitializeDatabaseAsync();
		}

		public async Task Start(CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					foreach (var (id, session) in _server._sessions)
					{
						string? request;
						if (session.RecvQueue.TryDequeue(out request))
						{
							Console.WriteLine($"request:{request}");
							if (request.StartsWith("POST"))
							{
								var format = HttpRequest.Load(request);
								var responce = new HttpResponce();
								try
								{
									switch (format.Directory)
									{
										case "/account":
											var json = JObject.Parse(format.Content);
											string? head = (string?)json["header"];
											if (head is null)
											{
												return;  // フォーマット違うから返さない
											}
											switch (head)
											{
												case "trypass":
													string? name = (string?)json["name"];
													if (name is null)
													{
														return;  // フォーマット違うから返さない
													}
													string? passwordHash = (string?)json["passwordHash"];
													if (passwordHash is null)
													{
														return;  // フォーマット違うから返さない
													}
													bool succeedLogin = false;
													if (await _dBConnector.ExistsAsync(name))
													{
														succeedLogin = await _dBConnector.VerifyPasswordAsync(name, passwordHash);
													}
													else  // ユーザー新規登録
													{
														succeedLogin = await _dBConnector.RegisterAsync(name, passwordHash);
													}

													if (succeedLogin == false)
													{
														// TODO: 失敗原因は告げないまま？
														responce.SetContent("Feild.");
														responce.StatusCode = 401;
														responce.Status = "Unauthorized";
														break;
													}

													await _dBConnector.IncrementLoginCountAsync(name);

													int loginCount = await _dBConnector.GetLoginCountAsync(name);


													var resJson = new JObject();
													resJson["header"] = "success";
													responce.SetContent(resJson.ToString());
													responce.SetContentTypeByExtension(".json");
													break;
												default:
													return;  // フォーマット違うから返さない
											}
											break;
										default:
											return;  // 知らんディレクトリへのアクセスは無視
									}

									responce.StoreResponce(out var buffer);
									session.SendQueue.Enqueue(buffer);
								}
								catch (Exception ex)
								{
									Console.WriteLine($"POST responce Error:{ex}");
								}
							}
							else if (request.StartsWith("GET"))
							{
								var format = HttpRequest.Load(request);
								Console.WriteLine($"format.Method={format.Method}, format.Directory={format.Directory}, format.Version={format.Version}");
								Console.WriteLine(new string('-', 30));
								var responce = new HttpResponce();
								try
								{
									switch (format.Directory)
									{
										case "/":
											format.Directory = "/Index.html";
											break;
										case "/world":
											format.Directory = "/world.html";
											break;
										case "/account":
											format.Directory = "/login.html";
											break;
										case "/images/Discord-Logo-Blurple.png":
										case "/index.css":
										case "/Index.html":
											break;
										default:
											format.Directory = Path.GetExtension(format.Directory) switch
											{
												".png" => "/images/Discord-Logo-Blurple.png",
												".css" => "/index.css",
												_ => "/Index.html"
											};
											break;  // ちゃんと受け取ったる！
											//return;  // 知らんディレクトリへのアクセスは無視
									}

									string indexHtmlPath = Path.GetFullPath(Directory.GetCurrentDirectory() + ("../../../.././Public") + format.Directory);
									string fileExtension = Path.GetExtension(indexHtmlPath).ToLower();
									responce.SetContentTypeByExtension(fileExtension);
									bool useBinary = fileExtension switch
									{
										".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".ico" => true,
										_ => false
									};
									if (useBinary)
									{
										responce.Content = File.ReadAllBytes(indexHtmlPath);
									}
									else
									{
										responce.SetContent(File.ReadAllText(indexHtmlPath, System.Text.Encoding.UTF8));
									}
									responce.StoreResponce(out var buffer);
									session.SendQueue.Enqueue(buffer);
								}
								catch (Exception ex)
								{
									Console.WriteLine($"FileAccessError:{ex}");
								}
							}
							else if (request.StartsWith("{"))
							{
								var json = JObject.Parse(request);
								string? head = (string?)json["head"];
								if (head is null)
								{
									continue;
								}
							}
						}
					}

					Console.Write(".");

					await Task.Delay(intervalMillSec, cancellationToken);
				}
			}
			catch (TaskCanceledException ex)
			{
				return;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
			}
		}
	}
}
