using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CHERRY.Services
{
	public class ProfileApiService
	{
		private readonly HttpClient _http;
		private readonly AuthService _auth;

		public ProfileApiService(HttpClient http, AuthService auth)
		{
			_http = http;
			_auth = auth;
		}

		public class UserProfileDto
		{
			public string UserId { get; set; } = string.Empty;
			public string? Nickname { get; set; }
			public int PeriodLength { get; set; }
			public int CycleLength { get; set; }
			public DateOnly? DateOfBirth { get; set; }
			public string? ProfileImageUrl { get; set; }
		}

		public async Task<UserProfileDto?> GetProfileAsync()
		{
			if (!await _auth.EnsureAuthHeaderAsync()) return null;
			return await _http.GetFromJsonAsync<UserProfileDto>("api/users/profile");
		}

		public async Task<bool> UpdateProfileAsync(UserProfileDto profile)
		{
			if (!await _auth.EnsureAuthHeaderAsync()) return false;
			var res = await _http.PutAsJsonAsync("api/users/profile", profile);
			return res.IsSuccessStatusCode;
		}

		public async Task<string?> UploadProfileImageAsync(Stream contentStream, string fileName, string contentType)
		{
			if (!await _auth.EnsureAuthHeaderAsync()) return null;
			using var form = new MultipartFormDataContent();
			var streamContent = new StreamContent(contentStream);
			streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
			form.Add(streamContent, "file", fileName);
			var res = await _http.PostAsync("api/users/profile/image", form);
			if (!res.IsSuccessStatusCode) return null;
			var json = await res.Content.ReadAsStringAsync();
			var payload = JsonSerializer.Deserialize<UploadResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			if (payload == null || string.IsNullOrWhiteSpace(payload.Url)) return null;
			return new Uri(_http.BaseAddress!, payload.Url).ToString();
		}

		public async Task<bool> DeleteProfileAsync()
		{
			if (!await _auth.EnsureAuthHeaderAsync()) return false;
			var res = await _http.DeleteAsync("api/users/profile");
			return res.IsSuccessStatusCode;
		}

		private class UploadResponse { public string Url { get; set; } = string.Empty; }
	}
}


