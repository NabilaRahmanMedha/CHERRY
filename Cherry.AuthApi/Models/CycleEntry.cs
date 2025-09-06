using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cherry.AuthApi.Models
{
	public class CycleEntry
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = default!;

		[ForeignKey(nameof(UserId))]
		public ApplicationUser? User { get; set; }

		[Required]
		public DateOnly StartDate { get; set; }

		[Required]
		public DateOnly EndDate { get; set; }
	}
}


