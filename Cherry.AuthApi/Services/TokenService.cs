using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cherry.AuthApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cherry.AuthApi
{
	public class TokenService
	{
		private readonly JwtOptions _options;
		public TokenService(IOptions<JwtOptions> options) { _options = options.Value; }

		public string CreateAccessToken(ApplicationUser user, string role)
		{
			var claims = new List<Claim>
			{
				new(JwtRegisteredClaimNames.Sub, user.Id),
				new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
				new(ClaimTypes.NameIdentifier, user.Id),
				new(ClaimTypes.Role, role)
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _options.Issuer,
				audience: _options.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
				signingCredentials: creds
			);
			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
	}
}


