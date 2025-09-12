using System;

namespace CHERRY.Services
{
	public interface INotificationService
	{
		void ShowOrUpdatePersistent(string notificationId, string title, string message);
		void Cancel(string notificationId);
	}

	public class NoopNotificationService : INotificationService
	{
		public void ShowOrUpdatePersistent(string notificationId, string title, string message) { }
		public void Cancel(string notificationId) { }
	}
}


