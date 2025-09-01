using System.ComponentModel.DataAnnotations;

namespace Cherry.AuthApi.Models
{
	public class UserProfile
	{
		[Key]
		public string UserId { get; set; } = default!;
		public ApplicationUser? User { get; set; }

		public string? Nickname { get; set; }
		public int PeriodLength { get; set; }
		public int CycleLength { get; set; }
		public DateOnly? DateOfBirth { get; set; }
		public string? ProfileImageUrl { get; set; }
	}
}


