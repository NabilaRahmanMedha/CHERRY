using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using CHERRY.Services;

namespace CHERRY.Platforms.Android.Services
{
	public class AndroidNotificationService : INotificationService
	{
		const string ChannelId = "cherry_predictions";
		const int DefaultId = 1001;

		public AndroidNotificationService()
		{
			CreateChannel();
		}

		public void ShowOrUpdatePersistent(string notificationId, string title, string message)
		{
			var context = Application.Context!;
			int id = GetId(notificationId);

			var pendingIntent = PendingIntent.GetActivity(
				context,
				0,
				new Intent(context, typeof(MainActivity)),
				PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

			var builder = new NotificationCompat.Builder(context, ChannelId)
				.SetContentTitle(title)
				.SetContentText(message)
				.SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
				.SetSmallIcon(Resource.Drawable.ic_launcher_foreground)
				.SetOngoing(true)
				.SetContentIntent(pendingIntent)
				.SetOnlyAlertOnce(true)
				.SetCategory(Notification.CategoryRecommendation)
				.SetPriority((int)NotificationPriority.Low);

			NotificationManagerCompat.From(context).Notify(id, builder.Build());
		}

		public void Cancel(string notificationId)
		{
			var context = Application.Context!;
			int id = GetId(notificationId);
			NotificationManagerCompat.From(context).Cancel(id);
		}

		void CreateChannel()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
			var channel = new NotificationChannel(ChannelId, "Cherry Predictions", NotificationImportance.Low)
			{
				Description = "Daily cycle predictions and tips"
			};
			var manager = (NotificationManager)Application.Context!.GetSystemService(Context.NotificationService)!;
			manager.CreateNotificationChannel(channel);
		}

		int GetId(string key)
		{
			if (string.IsNullOrEmpty(key)) return DefaultId;
			unchecked
			{
				return DefaultId + key.GetHashCode();
			}
		}
	}
}


