using Microsoft.Maui;

namespace CHERRY.Services
{
	public static class ServiceHelper
	{
		public static T GetService<T>() where T : notnull
		{
			return Current.GetService<T>();
		}

		private static IServiceProvider Current => Application.Current!.Handler!.MauiContext!.Services;
	}
}


