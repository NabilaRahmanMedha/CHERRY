using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace CHERRY.Services
{
	public class AuthService
	{
		private readonly HttpClient _http;
		private const string AccessTokenKey = "auth_access_token";
		private const string RefreshTokenKey = "auth_refresh_token";
		private const string EmailKey = "auth_email";

		public AuthService(HttpClient http) { _http = http; }

		public async Task<bool> RegisterAsync(string email, string password, string role = "User")
		{
			var res = await _http.PostAsJsonAsync("api/auth/register", new { email, password, role });
			if (!res.IsSuccessStatusCode) return false;
			return await StoreTokensAsync(res, email);
		}

		public async Task<bool> LoginAsync(string email, string password)
		{
			var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
			if (!res.IsSuccessStatusCode) return false;
			return await StoreTokensAsync(res, email);
		}


		public Task LogoutAsync()
		{
			SecureStorage.Default.Remove(AccessTokenKey);
			SecureStorage.Default.Remove(RefreshTokenKey);
			SecureStorage.Default.Remove(EmailKey);
			return Task.CompletedTask;
		}

		public async Task<string?> GetAccessTokenAsync() => await SecureStorage.Default.GetAsync(AccessTokenKey);

		public async Task<string?> GetEmailAsync() => await SecureStorage.Default.GetAsync(EmailKey);

		public async Task<bool> EnsureAuthHeaderAsync()
		{
			var token = await GetAccessTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) return false;
			_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			return true;
		}

		public async Task<bool> RefreshAsync()
		{
			var refresh = await SecureStorage.Default.GetAsync(RefreshTokenKey);
			var email = await GetEmailAsync();
			if (string.IsNullOrEmpty(refresh) || string.IsNullOrEmpty(email)) return false;

			var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
			{
				Content = JsonContent.Create(new { refreshToken = refresh })
			};
			req.Headers.Add("X-User-Email", email);
			var res = await _http.SendAsync(req);
			if (!res.IsSuccessStatusCode) return false;
			return await StoreTokensAsync(res, email);
		}

		private async Task<bool> StoreTokensAsync(HttpResponseMessage res, string email)
		{
			var payload = JsonSerializer.Deserialize<AuthResponseDto>(await res.Content.ReadAsStringAsync(),
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			if (payload == null) return false;

			await SecureStorage.Default.SetAsync(AccessTokenKey, payload.AccessToken);
			await SecureStorage.Default.SetAsync(RefreshTokenKey, payload.RefreshToken);
			await SecureStorage.Default.SetAsync(EmailKey, email);
			_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
			return true;
		}

		private class AuthResponseDto
		{
			public string AccessToken { get; set; } = "";
			public string RefreshToken { get; set; } = "";
			public string UserId { get; set; } = "";
			public string Email { get; set; } = "";
			public string Role { get; set; } = "User";
		}
	}
}


