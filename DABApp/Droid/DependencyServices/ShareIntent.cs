using System;
using Android.Content;
using DABApp.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(ShareIntent))]
namespace DABApp.Droid
{
	public class ShareIntent: IShareable
	{

		public void OpenShareIntent(string Channelcode, string episodeId)
		{
			var myIntent = new Intent(Android.Content.Intent.ActionSend);
			myIntent.PutExtra(Intent.ExtraText, $"https://player.dailyaudiobible.com/{Channelcode}/{episodeId}");
			myIntent.SetType("text/plain");
			Forms.Context.StartActivity(Intent.CreateChooser(myIntent, "Choose an App"));
		}
	}
}
