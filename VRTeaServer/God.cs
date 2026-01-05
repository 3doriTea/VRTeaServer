using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using VRTeaServer.DB;
using VRTeaServer.Web;

using static VRTeaServer.JsonUtility;

namespace VRTeaServer
{
	internal class God
	{
		private const int TryCount = 3;  // 失敗してリトライするカウント

		private int intervalMillSec = 10;
		private World _world;
		private Server _server;
		private ConcurrentDictionary<string, ActiveUser> _activeUsers = [];
		private DBConnector _dBConnector = new();
		private Reaper _reaper;
		private AI _ai;

		private int _anonymousCount = -1;

		private class GodJsonRequestException : Exception
		{
			// 単体例外
			public GodJsonRequestException(string message) : base(message) { }
			// 内部例外を受け取りつつ
			public GodJsonRequestException(string message, Exception innner) : base(message, innner) { }
		}

		public God(World world, Server server)
		{
			_world = world;
			_server = server;
			_reaper = new Reaper(server, 5.0f);
			_ai = new AI();

			_dBConnector.InitializeDatabaseAsync().Wait();

			_server.OnDisconnected += disconnectedSessionId =>
			{
				// 切断したユーザーを消す
				int disconnectedUserId;
				if (!_world.SessionIdToUserId.TryRemove(disconnectedSessionId, out disconnectedUserId))
				{
					return;  // ゲームプレイヤー以外が切断したなら無視
				}

				JObject sendJson = JObject.FromObject(new
				{
					head = "event",
					body = new
					{
						handle = "disconnected",
						userId = disconnectedUserId,
					},
				});

				// 必ず！直接！ 送信。
				string jsonStr = sendJson.ToString();
				int jsonSize = Encoding.UTF8.GetByteCount(jsonStr);

				Console.WriteLine($"Send:{jsonStr}");
				Console.WriteLine($"Leave:{disconnectedSessionId}");

				byte[] sendBuffer = new byte[jsonSize + sizeof(int)];
				// クライアントはWindowsだから必ずリトルエンディアンにする！
				BinaryPrimitives.WriteInt32LittleEndian(sendBuffer, jsonSize);
				Encoding.UTF8.GetBytes(jsonStr).AsSpan().CopyTo(sendBuffer.AsSpan(sizeof(int)));

				foreach (var (id, session) in _server._sessions)
				{
					if (_world.SessionIdToUserId.ContainsKey(id) == false)
					{
						// 送信先がゲームに参加していないセッションには送らない
						continue;
					}
					if (disconnectedUserId == id)
					{
						// 切断したのが自分自身なら無視
						continue;
					}

					session.SendQueue.Enqueue(sendBuffer);
				}
			};
		}

		public async Task Start(CancellationTokenSource cts)
		{
			 _ = _reaper.Start(cts);

			async Task RequestProc(string request, int sessionId, Session session)
			{
				int tryCount = 0;
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
							case "/images/MainIcon.png":
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

						string indexHtmlPath = "";
						if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
						{
							indexHtmlPath = Path.GetFullPath(Directory.GetCurrentDirectory() + ("../../../.././Public") + format.Directory);
						}
						else
						{
							indexHtmlPath = Path.GetFullPath(Directory.GetCurrentDirectory() + ("/Public/") + format.Directory);
						}
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
					Console.WriteLine(request);

					JObject? json = null;
					try
					{
						json = JObject.Parse(request);
					}
					catch
					{
						Console.WriteLine("Failed parse request json");
						return;  // 変換できないため無視
					}
					string? head = (string?)json["head"];
					if (head is null)
					{
						return;  // ヘッドがないため無視
					}

					var respJson = new JObject();

					GodJsonRequestException Error(string message, string hint = "idk.")
					{
						if (!respJson.TryGet<string>("head", out var head))
						{
							head = "?";
						}

						respJson = new()
						{
							["head"] = "error",
							["body"] = message,
							["hint"] = hint,
							["dist"] = head
						};
						return new GodJsonRequestException(message);
					}

					try
					{
						switch (head)
						{
							case "update":
							{
								respJson["head"] = "updated";

								if (!json.TryGet<int>("id", out var userId))
								{
									throw Error("Missing request \"id\"");
								}

								if (json["body"] is null)
								{
									throw Error("Missing request \"body\"");
								}

								var body = json["body"];


								if (!body.TryGet<float>("positionX", out var posX))
								{
									throw Error("Missing request \"body.position.x\"");
								}
								if (!body.TryGet<float>("positionY", out var posY))
								{
									throw Error("Missing request \"body.position.y\"");
								}
								if (!body.TryGet<float>("positionZ", out var posZ))
								{
									throw Error("Missing request \"body.position.z\"");
								}
								if (!body.TryGet<int>("positionW", out var posW))
								{
									throw Error("Missing request \"body.position.w\"");
								}
								if (!body.TryGet<float>("rotationY", out var rotY))
								{
									throw Error("Missing request \"body.rotationY\"");
								}

								if (!_world.SessionIdToUserId.TryGetValue(sessionId, out var userIdFromSessionId))
								{
									throw Error("Not logged in");
								}

								if (!_world.Players.TryGetValue(userId, out var player))
								{
									throw Error("conflict get value");
								}

								if (userIdFromSessionId != player.Id)
								{
									throw Error("pls logout that account. reason: Your account may have been hijacked, or you may be the culprit.");
								}

								if (!_world.Players.TryUpdate(userId, new Player
									{ Id = player.Id, Name = player.Name, PositionX = posX, PositionY = posY, PositionZ = posZ, PositionW = posW, RotationY = rotY }, player))
								{
									throw Error("conflict update");
								}

								if (respJson["body"] is null)
								{
									respJson["body"] = new JObject();
								}

								foreach (var (pSessionId, pUserId) in _world.SessionIdToUserId)
								{
									if (pUserId == userId)
									{
										continue;  // 自分の情報はしらない
									}

									Player? playerData = null;

									tryCount = 0;
									while (!_world.Players.TryGetValue(pUserId, out playerData)
										&& tryCount < TryCount)
									{
										tryCount++;
										await Task.Delay(1, cts.Token);
									}

									if (tryCount >= TryCount)
									{
										throw Error("conflict Players dict");
									}

									respJson["body"]![$"{pUserId}"] = JObject.FromObject(playerData!);
								}

								break;
							}
							case "join":
								respJson["head"] = "joined";

								int joinedUserId = _anonymousCount;
								_anonymousCount--;

								string joinedUserName = NameGenerator.Generate(joinedUserId);
								respJson["name"] = joinedUserName;
								respJson["id"] = joinedUserId;

								tryCount = 0;
								// 何回か試す
								while (!_world.SessionIdToUserId.TryAdd(sessionId, joinedUserId)
									&& tryCount < TryCount)
								{
									tryCount++;
									await Task.Delay(1, cts.Token);
								}

								if (tryCount >= TryCount)
								{
									throw Error("conflict NameToId dict");
								}

								tryCount = 0;
								// 何回か試す
								while (!_world.Players.TryAdd(joinedUserId, new Player{ Id = joinedUserId, Name = joinedUserName })
									&& tryCount < TryCount)
								{
									tryCount++;
									await Task.Delay(1, cts.Token);
								}

								if (tryCount >= TryCount)
								{
									throw Error("conflict players dict");
								}

								break;
							case "ask":
								respJson["head"] = "asked";
								break;
							default:
								respJson["head"] = "\\_ツ_/";
								break;
						}
					}
					catch (GodJsonRequestException ex)
					{
						_ = ex;
					}
					catch (Exception ex)
					{
						Console.WriteLine($"God Json request error:{ex}");
					}
					finally
					{
						// 必ず！直接！ 送信。
						string jsonStr = respJson.ToString();
						int jsonSize = Encoding.UTF8.GetByteCount(jsonStr);

						Console.WriteLine($"Send:{jsonStr}");

						byte[] sendBuffer = new byte[jsonSize + sizeof(int)];
						// クライアントはWindowsだから必ずリトルエンディアンにする！
						BinaryPrimitives.WriteInt32LittleEndian(sendBuffer, jsonSize);
						Encoding.UTF8.GetBytes(jsonStr).AsSpan().CopyTo(sendBuffer.AsSpan(sizeof(int)));

						session.SendQueue.Enqueue(sendBuffer);
					}
				}
			}

			try
			{
				while (true)
				{
					foreach (var (id, session) in _server._sessions)
					{
						if (session.RecvQueue.TryDequeue(out var request))
						{
							await RequestProc(request, id, session);
						}
					}

					Console.Write($".{_server._sessions.Count}-{_world.SessionIdToUserId.Count}");

					await Task.Delay(intervalMillSec, cts.Token);
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
