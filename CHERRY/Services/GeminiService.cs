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

		// Gemini REST endpoint models
		private const string PrimaryModel = "gemini-1.5-flash";
		private static readonly string[] FallbackModels = new[] { "gemini-1.5-flash-8b", "gemini-1.5-flash-latest" };

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

			var modelsToTry = new List<string>();
			modelsToTry.Add(PrimaryModel);
			modelsToTry.AddRange(FallbackModels);

			var systemInstruction = "You are CherryMate, a friendly, empathetic assistant focused on menstruation, cycles, PMS, cramps, symptoms, hygiene, nutrition, mental health, and when to seek medical help. Avoid explicit sexual content. Offer supportive, culturally sensitive guidance. Avoid diagnosing; advise seeing a professional for urgent or severe symptoms. Keep answers concise for mobile.";

			var contents = new List<object>();
			// Provide a soft system prompt by starting conversation with assistant persona
			contents.Add(new { role = "model", parts = new[] { new { text = systemInstruction } } });

			foreach (var (isUser, content) in history)
			{
				contents.Add(new
				{
					role = isUser ? "user" : "model",
					parts = new[] { new { text = content } }
				});
			}


			foreach (var model in modelsToTry)
			{
				var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
				var payload = new { contents };

				var attempt = 0;
				while (attempt < 3)
				{
					attempt++;
					using var request = new HttpRequestMessage(HttpMethod.Post, url)
					{
						Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json")
					};

					HttpResponseMessage response;
					try
					{
						response = await _httpClient.SendAsync(request);
					}
					catch
					{
						if (attempt >= 3) break;
						await Task.Delay(300 * attempt + Random.Shared.Next(0, 200));
						continue;
					}

					var json = await response.Content.ReadAsStringAsync();
					if (!response.IsSuccessStatusCode)
					{
						var shouldRetry = (int)response.StatusCode == 429 || (int)response.StatusCode == 503;
						var overloaded = false;
						try
						{
							using var errDoc = JsonDocument.Parse(json);
							var msg = errDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
							overloaded = msg != null && msg.Contains("overloaded", StringComparison.OrdinalIgnoreCase);
							if (!shouldRetry && !overloaded)
								return $"Sorry, the AI service returned an error: {msg}";
						}
						catch { }

						if (attempt < 3 && (shouldRetry || overloaded))
						{
							await Task.Delay(400 * (int)Math.Pow(2, attempt - 1) + Random.Shared.Next(0, 250));
							continue;
						}

						break;
					}

					using var doc = JsonDocument.Parse(json);
					var root = doc.RootElement;
					if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
					{
						var first = candidates[0];
						if (first.TryGetProperty("content", out var contentEl) && contentEl.TryGetProperty("parts", out var partsEl) && partsEl.ValueKind == JsonValueKind.Array && partsEl.GetArrayLength() > 0)
						{
							var text = partsEl[0].GetProperty("text").GetString();
							if (!string.IsNullOrWhiteSpace(text)) return text!;
						}
					}

					if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out var blockReason))
					{
						return "I can't respond to that directly. Let's keep things safe and respectful. If you have questions about sexual health related to your cycle, I can offer general guidance and point to reliable resources.";
					}

					// If no usable content, try next attempt/model
				}
			}

			return "Sorry, the model is busy right now. I tried a few times and will be ready if you try again in a moment.";
		}
	}
}


