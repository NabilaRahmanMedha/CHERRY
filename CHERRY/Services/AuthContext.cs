using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CHERRY.Services
{
	public static class AuthContext
	{
		public static string? GetRoleFromJwt(string jwt)
		{
			var handler = new JwtSecurityTokenHandler();
			var token = handler.ReadJwtToken(jwt);
			return token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
		}
	}
}


