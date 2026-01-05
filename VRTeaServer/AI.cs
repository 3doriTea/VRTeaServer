using GroqApiLibrary;
using System.Text.Json.Nodes;

namespace VRTeaServer
{
	internal class AI
	{
		private readonly GroqApiClient _groqApi;

		public AI(string apiKey)
		{
			_groqApi = new GroqApiClient(apiKey);
		}

		public async Task<string?> Ask(string content)
		{
			var request = new JsonObject
			{
				["model"] = "llama3-8b-8192", // または "llama3-70b-8192"
				["messages"] = new JsonArray
				{
					new JsonObject
					{
						["role"] = "system",
						["content"] = string.Join("\n",
							[
								"日本語で喋る",
								"足車輪",
								"おしとやかな性格",
								"年齢38歳",
								"性別は中性",
								"声のトーンは1.08",
								"趣味はお手玉",
								"好きな色は赤紫色",
								"一人称は「俺っち」",
								"語尾は「ピカ」",
							])
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

			try
			{
				var result = await _groqApi.CreateChatCompletionAsync(request);
				var responseText = result?["choices"]?[0]?["message"]?["content"]?.ToString();

				Console.WriteLine("AI Answer:");
				Console.WriteLine(responseText);
				return responseText!;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex}");
				return null;
			}
		}
	}
}
