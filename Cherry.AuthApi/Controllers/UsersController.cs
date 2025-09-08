using Cherry.AuthApi.Data;
using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Cherry.AuthApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UsersController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IWebHostEnvironment _env;
		public UsersController(AppDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
		{
			_db = db;
			_userManager = userManager;
			_env = env;
		}

		[Authorize]
		[HttpGet("profile")]
		public async Task<ActionResult<UserProfile>> GetProfile()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
			var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
			if (profile == null) return NotFound();
			return profile;
		}

		[Authorize]
		[HttpPut("profile")]
		public async Task<IActionResult> UpdateProfile(UserProfile profile)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
			if (profile.UserId != userId) return Forbid();

			_db.UserProfiles.Update(profile);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		[Authorize]
		[HttpPost("profile/image")]
		[RequestSizeLimit(10_000_000)]
		public async Task<ActionResult<object>> UploadProfileImage([FromForm] IFormFile file)
		{
			if (file == null || file.Length == 0) return BadRequest("No file uploaded");

			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
			var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
			if (profile == null) return NotFound();

			var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles");
			Directory.CreateDirectory(uploadsRoot);

			var extension = Path.GetExtension(file.FileName);
			if (string.IsNullOrEmpty(extension)) extension = ".jpg";
			var fileName = $"{userId}{extension}";
			var filePath = Path.Combine(uploadsRoot, fileName);

			using (var stream = System.IO.File.Create(filePath))
			{
				await file.CopyToAsync(stream);
			}

			var relativeUrl = $"/uploads/profiles/{fileName}";
			profile.ProfileImageUrl = relativeUrl;
			await _db.SaveChangesAsync();

			return new { url = relativeUrl };
		}

		[Authorize]
		[HttpDelete("profile")]
		public async Task<IActionResult> DeleteProfile()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

			var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
			if (profile != null)
			{
				if (!string.IsNullOrWhiteSpace(profile.ProfileImageUrl))
				{
					var pathPart = profile.ProfileImageUrl.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
					var fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), pathPart);
					if (System.IO.File.Exists(fullPath))
					{
						System.IO.File.Delete(fullPath);
					}
				}
				_db.UserProfiles.Remove(profile);
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user != null)
			{
				var result = await _userManager.DeleteAsync(user);
				if (!result.Succeeded) return BadRequest(result.Errors);
			}

			await _db.SaveChangesAsync();
			return NoContent();
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IEnumerable<object>> GetAll() =>
			await _db.Users.Select(u => new { u.Id, u.Email }).ToListAsync();
	}
}


