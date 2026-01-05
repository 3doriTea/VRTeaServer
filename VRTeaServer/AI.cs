using GroqApiLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace VRTeaServer
{
	internal class AI
	{
		public AI() { }
		public async Task Ask(string content)
		{
			

			string apiKey = "あなたのAPIキー";
			var groqApi = new GroqApiClient(apiKey);

			// 2. リクエスト内容（JSON形式）の作成
			var request = new JsonObject
			{
				["model"] = "llama3-8b-8192", // または "llama3-70b-8192"
				["messages"] = new JsonArray
				{
					new JsonObject
					{
						["role"] = "system",
						["content"] = "あなたは有能なアシスタントです。"
					},
					new JsonObject
					{
						["role"] = "user",
						["content"] = $"{content}",
					}
				},
				["temperature"] = 0.5,
				["max_tokens"] = 1024
			};

			// 3. APIを呼び出して結果を表示
			try
			{
				var result = await groqApi.CreateChatCompletionAsync(request);
				// レスポンスからテキスト部分を抽出
				var responseText = result?["choices"]?[0]?["message"]?["content"]?.ToString();

				Console.WriteLine("AIの回答:");
				Console.WriteLine(responseText);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"エラーが発生しました: {ex.Message}");
			}
		}
	}
}
