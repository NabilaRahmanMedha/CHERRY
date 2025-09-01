namespace Cherry.AuthApi.Models
{
	public class JwtOptions
	{
		public string Issuer { get; set; } = string.Empty;
		public string Audience { get; set; } = string.Empty;
		public string Secret { get; set; } = string.Empty;
		public int AccessTokenMinutes { get; set; } = 15;
		public int RefreshTokenDays { get; set; } = 14;
	}
}


