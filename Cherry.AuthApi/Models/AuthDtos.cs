namespace Cherry.AuthApi.Models
{
	public record RegisterRequest(string Email, string Password, string? Role = "User");
	public record LoginRequest(string Email, string Password);
	public record AuthResponse(string AccessToken, string RefreshToken, string UserId, string Email, string Role);
	public record RefreshRequest(string RefreshToken);
}


