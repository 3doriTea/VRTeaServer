using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
			_dBConnector.InitializeDatabaseAsync().Wait();
		}

		public async Task Start(CancellationToken cancellationToken)
		{
			async Task RequestProc(string request, int id, Session session)
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
								JObject? json = null;
								try
								{
									json = JObject.Parse(format.Content);
								}
								catch (JsonReaderException ex)
								{
									return;  // フォーマット違うから返さない
								}
								string? head = (string?)json["header"];
								if (head is null)
								{
									return;  // フォーマット違うから返さない
								}

								var resJson = new JObject();
								resJson["header"] = "success";

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

										// ログインカウントプラス
										await _dBConnector.IncrementLoginCountAsync(name);

										int loginCount = await _dBConnector.GetLoginCountAsync(name);

										char[] sign = ['!', '?', '%', '$', '#', '-', ':', ';', '.', ',', '\'', '\"', '&', '(', ')', '^', '*', '+', '/', '<', '>'];
										char prefix = sign[loginCount % sign.Length];
										char postfix = sign[(loginCount * loginCount) % sign.Length];
										byte[] bytes = Encoding.UTF8.GetBytes($"{prefix}{loginCount}{name}{postfix}");

										byte[] hashBytes = SHA256.HashData(bytes);

										string newSessionHash = Convert.ToHexString(hashBytes).ToLower();

										_activeUsers.TryAdd(newSessionHash, new ActiveUser { Name = name });

										resJson["sessionHash"] = newSessionHash;

										responce.SetContent(resJson.ToString());
										responce.SetContentTypeByExtension(".json");
										break;
									case "getinfo":
										string? sessionHash = (string?)json["sessionHash"];
										if (sessionHash is null)
										{
											return;  // フォーマット違うから返さない
										}
										string? at = (string?)json["at"];
										if (at is null)
										{
											return;  // フォーマット違うから返さない
										}

										if (_activeUsers.ContainsKey(sessionHash) == false)
										{
											// セッションがリセットされたから失敗
											responce.SetContent("Feild.");
											responce.StatusCode = 401;
											responce.Status = "Unauthorized";
											break;
										}

										switch (at)
										{
											case "name":
												resJson["content"] = _activeUsers[sessionHash].Name;

												responce.SetContent(resJson.ToString());
												responce.SetContentTypeByExtension(".json");
												break;
											default:
												// セッションがリセットされたから失敗
												responce.SetContent("Dont access at");
												responce.StatusCode = 401;
												responce.Status = "Unauthorized";
												break;
										}

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
						return;
					}
				}
			}

			try
			{
				while (true)
				{
					foreach (var (id, session) in _server._sessions)
					{
						string? request;
						if (session.RecvQueue.TryDequeue(out request))
						{
							await RequestProc(request, id, session);
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
