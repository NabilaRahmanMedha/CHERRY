using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CHERRY.Services
{
	public class GeminiService
	{
		private readonly HttpClient _httpClient;
		private string _apiKey;

		// Gemini REST endpoint for text generation (Gemini 1.5 Flash recommended for chat)
		private const string Model = "gemini-1.5-flash";

		public GeminiService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_apiKey = "AIzaSyAXlrEtKZxWbr7hoCGmd-EYvoXh0D9u7vw"; // injected later via Configure
		}

		public void Configure(string apiKey)
		{
			_apiKey = apiKey;
		}

		private static readonly JsonSerializerOptions JsonOpts = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
		};

		public async Task<string> GetChatCompletionAsync(IEnumerable<(bool isUser, string content)> history)
		{
			if (string.IsNullOrWhiteSpace(_apiKey))
				throw new InvalidOperationException("Gemini API key not configured");

			var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey}";

			var safetyInstruction = "You are CherryMate, a friendly, empathetic assistant focused on menstruation, cycles, PMS, cramps, symptoms, hygiene, nutrition, mental health, and when to seek medical help. Offer supportive, culturally sensitive guidance. Avoid diagnosing; advise seeing a professional for urgent or severe symptoms. Keep answers concise for mobile.";

			var parts = new List<object>();
			// System-style instruction as the first block
			parts.Add(new { text = safetyInstruction });

			foreach (var (isUser, content) in history)
			{
				parts.Add(new { text = (isUser ? "User: " : "Assistant: ") + content });
			}

			var payload = new
			{
				contents = new[]
				{
					new
					{
						role = "user",
						parts = parts
					}
				}
			};

			using var request = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json")
			};

			var response = await _httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();
			var json = await response.Content.ReadAsStringAsync();

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			var candidate = root.GetProperty("candidates")[0];
			var text = candidate.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
			return text ?? "";
		}
	}
}


