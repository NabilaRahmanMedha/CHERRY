using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using CHERRY.Services;
using Microsoft.Maui.ApplicationModel;

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
			var context = global::Android.App.Application.Context!;
			RequestNotificationPermissionIfNeeded();
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
				.SetSmallIcon(Resource.Mipmap.appicon)
				.SetOngoing(true)
				.SetContentIntent(pendingIntent)
				.SetOnlyAlertOnce(true)
				.SetCategory(NotificationCompat.CategoryRecommendation)
				.SetPriority((int)NotificationPriority.Low);

			NotificationManagerCompat.From(context).Notify(id, builder.Build());
		}

		public void Cancel(string notificationId)
		{
			var context = global::Android.App.Application.Context!;
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
			var manager = (NotificationManager)global::Android.App.Application.Context!.GetSystemService(Context.NotificationService)!;
			manager.CreateNotificationChannel(channel);
		}

		void RequestNotificationPermissionIfNeeded()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) return;
			var activity = Platform.CurrentActivity;
			if (activity == null) return;
			if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.PostNotifications) == global::Android.Content.PM.Permission.Granted)
				return;

			AndroidX.Core.App.ActivityCompat.RequestPermissions(activity, new string[] { global::Android.Manifest.Permission.PostNotifications }, 101);
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


