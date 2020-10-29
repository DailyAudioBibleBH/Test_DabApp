using System;
using Android.Content;
using DABApp.Droid;
using Plugin.CurrentActivity;
using Xamarin.Forms;

[assembly: Dependency(typeof(RivetsService))]
namespace DABApp.Droid
{
	public class RivetsService: IRivets
	{
		public void NavigateTo(string Url)
		{
			Intent browserIntent = new Intent(Intent.ActionView);
			browserIntent.SetData(Android.Net.Uri.Parse(Url));
			var activity = CrossCurrentActivity.Current.Activity;
			activity.StartActivity(browserIntent);
		}
	}
}
