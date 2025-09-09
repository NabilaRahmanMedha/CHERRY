using SQLite;

namespace CHERRY.Models
{
	public class ChatMessage
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		// Email of the user who owns this message
		[Indexed]
		public string UserEmail { get; set; } = string.Empty;

		// true if sent by the user, false if from the bot
		public bool IsUser { get; set; }

		// message text content
		public string Content { get; set; } = string.Empty;

		// UTC timestamp ticks
		public long CreatedUtcTicks { get; set; }
	}
}


