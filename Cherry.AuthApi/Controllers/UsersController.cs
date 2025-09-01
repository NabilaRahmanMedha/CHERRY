using Cherry.AuthApi.Data;
using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cherry.AuthApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UsersController : ControllerBase
	{
		private readonly AppDbContext _db;
		public UsersController(AppDbContext db) { _db = db; }

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

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IEnumerable<object>> GetAll() =>
			await _db.Users.Select(u => new { u.Id, u.Email }).ToListAsync();
	}
}


