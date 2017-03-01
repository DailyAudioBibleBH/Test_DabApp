using System;
using Xamarin.Forms;
using SlideOverKit;

namespace DABApp
{
	public static class GlobalResources
	{
		//public static bool IsPlaying { get; set; }
		//public static IAudio Player { get; set;}

		public static int FlowListViewColumns
		{
			//Returns the number of columnts to use in a FlowListView
			get
			{
				switch (Device.Idiom)
				{
					case TargetIdiom.Phone:
						return 2;
					case TargetIdiom.Tablet:
						return 3;
					default:
						return 2;
				}
			}
		}


		public static double ThumbnailImageHeight
		{
			//returns the height we should use for a square thumbnail (based on the idiom and screen WIDTH)
			get
			{
				double knownPadding = 30;
				return (App.Current.MainPage.Width  / FlowListViewColumns) - knownPadding;
			}
		}

	
	}
}
