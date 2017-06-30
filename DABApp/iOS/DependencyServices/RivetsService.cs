using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(RivetsService))]
namespace DABApp.iOS
{
	public class RivetsService: IRivets
	{
		public void NavigateTo(string Url) {
			UIApplication.SharedApplication.OpenUrl(new Foundation.NSUrl(Url));
		}
	}
}
