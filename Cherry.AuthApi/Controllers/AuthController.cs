using Cherry.AuthApi;
using Cherry.AuthApi.Data;
using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cherry.AuthApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly TokenService _tokenService;
		private readonly AppDbContext _db;

		public AuthController(UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			TokenService tokenService,
			AppDbContext db)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_tokenService = tokenService;
			_db = db;
		}

		[HttpPost("register")]
		public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
		{
			var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
			var result = await _userManager.CreateAsync(user, request.Password);
			if (!result.Succeeded) return BadRequest(result.Errors);

			var role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role!;
			if (!await _db.Roles.AnyAsync(r => r.Name == role)) role = "User";
			await _userManager.AddToRoleAsync(user, role);

			_db.UserProfiles.Add(new UserProfile { UserId = user.Id, Nickname = "", PeriodLength = 0, CycleLength = 0 });
			await _db.SaveChangesAsync();

			var access = _tokenService.CreateAccessToken(user, role);
			var refresh = _tokenService.CreateRefreshToken();
			Response.Cookies.Append("refreshToken", refresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(14) });
			return new AuthResponse(access, refresh, user.Id, user.Email!, role);
		}

		[HttpPost("login")]
		public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
		{
			var user = await _userManager.FindByEmailAsync(request.Email);
			if (user is null) return Unauthorized();

			var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
			if (!result.Succeeded) return Unauthorized();

			var roles = await _userManager.GetRolesAsync(user);
			var role = roles.FirstOrDefault() ?? "User";
			var access = _tokenService.CreateAccessToken(user, role);
			var refresh = _tokenService.CreateRefreshToken();
			Response.Cookies.Append("refreshToken", refresh, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(14) });
			return new AuthResponse(access, refresh, user.Id, user.Email!, role);
		}

		[HttpPost("refresh")]
		public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
		{
			var refresh = string.IsNullOrWhiteSpace(request.RefreshToken) ? Request.Cookies["refreshToken"] : request.RefreshToken;
			if (string.IsNullOrEmpty(refresh)) return Unauthorized();

			var email = Request.Headers["X-User-Email"].ToString();
			if (string.IsNullOrEmpty(email)) return BadRequest("Missing X-User-Email");

			var user = await _userManager.FindByEmailAsync(email);
			if (user is null) return Unauthorized();

			var roles = await _userManager.GetRolesAsync(user);
			var role = roles.FirstOrDefault() ?? "User";
			var access = _tokenService.CreateAccessToken(user, role);
			return new AuthResponse(access, refresh!, user.Id, user.Email!, role);
		}

		[Authorize]
		[HttpGet("me")]
		public async Task<ActionResult<object>> Me()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
			var user = await _userManager.FindByIdAsync(userId);
			var roles = await _userManager.GetRolesAsync(user!);
			var profile = await _db.UserProfiles.FindAsync(userId);
			return Ok(new { user!.Email, Roles = roles, Profile = profile });
		}
	}
}


