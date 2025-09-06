using System.Security.Claims;
using Cherry.AuthApi.Data;
using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cherry.AuthApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class CyclesController : ControllerBase
	{
		private readonly AppDbContext _db;
		public CyclesController(AppDbContext db) { _db = db; }

		[HttpGet("history")]
		public async Task<ActionResult<IEnumerable<CycleDto>>> GetHistory()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var entries = await _db.CycleEntries
				.Where(c => c.UserId == userId)
				.OrderByDescending(c => c.StartDate)
				.ToListAsync();

			return Ok(entries.Select(CycleDto.FromEntity));
		}

		[HttpGet("month")]
		public async Task<ActionResult<IEnumerable<CycleDto>>> GetByMonth([FromQuery] int year, [FromQuery] int month)
		{
			if (year < 1900 || month < 1 || month > 12) return BadRequest("Invalid year/month");
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var start = new DateOnly(year, month, 1);
			var end = start.AddMonths(1).AddDays(-1);

			var entries = await _db.CycleEntries
				.Where(c => c.UserId == userId &&
					(c.StartDate <= end && c.EndDate >= start))
				.OrderBy(c => c.StartDate)
				.ToListAsync();

			return Ok(entries.Select(CycleDto.FromEntity));
		}

		[HttpPost]
		public async Task<ActionResult<CycleDto>> Create([FromBody] CreateCycleDto dto)
		{
			if (dto.EndDate < dto.StartDate) return BadRequest("End date cannot be before start date.");
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var entity = new CycleEntry
			{
				UserId = userId,
				StartDate = dto.StartDate,
				EndDate = dto.EndDate
			};

			_db.CycleEntries.Add(entity);
			await _db.SaveChangesAsync();

			return CreatedAtAction(nameof(GetHistory), new { }, CycleDto.FromEntity(entity));
		}
	}

	public record CycleDto(DateOnly StartDate, DateOnly EndDate)
	{
		public static CycleDto FromEntity(CycleEntry e) => new(e.StartDate, e.EndDate);
	}

	public class CreateCycleDto
	{
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
	}
}


