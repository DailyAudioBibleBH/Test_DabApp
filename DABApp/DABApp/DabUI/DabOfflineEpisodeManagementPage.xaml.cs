using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabOfflineEpisodeManagementPage : DabBaseContentPage
	{

		public DabOfflineEpisodeManagementPage()
		{
			InitializeComponent();
			//base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			DabViewHelper.InitDabForm(this);
			switch (OfflineEpisodeSettings.Instance.Duration)
			{ 
				case "One Day":
					FirstIcon.IsVisible = true;
					break;
				case "Two Days":
					SecondIcon.IsVisible = true;
					break;
				case "Three Days":
					ThirdIcon.IsVisible = true;
					break;
				//case "FourDays":
				//	FourthIcon.IsVisible = true;
				//	break;
				case "One Week":
					FifthIcon.IsVisible = true;
					break;
				//case "One Month":
				//	SixthIcon.IsVisible = true;
				//	break;
			}
			AfterListening.On = OfflineEpisodeSettings.Instance.DeleteAfterListening;
		}

		void OnDeleteAfterListening(object o, ToggledEventArgs e) {
			var pre = e.Value;
			OfflineEpisodeSettings.Instance.DeleteAfterListening = pre;
			PlayerFeedAPI.UpdateOfflineEpisodeSettings();
		}

		void OnDurationPicked(object o, EventArgs e) {
			var item = (ViewCell)o;
			switch (item.AutomationId) { 
				case "OneDay":
					FirstIcon.IsVisible = true;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = false;
					//FourthIcon.IsVisible = false;
					FifthIcon.IsVisible = false;
					//SixthIcon.IsVisible = false;
					OfflineEpisodeSettings.Instance.Duration = "One Day";
					break;
				case "TwoDays":
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = true;
					ThirdIcon.IsVisible = false;
					//FourthIcon.IsVisible = false;
					FifthIcon.IsVisible = false;
					//SixthIcon.IsVisible = false;
					OfflineEpisodeSettings.Instance.Duration = "Two Days";
					break;
				case "ThreeDays":
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = true;
					//FourthIcon.IsVisible = false;
					FifthIcon.IsVisible = false;
					//SixthIcon.IsVisible = false;
					OfflineEpisodeSettings.Instance.Duration = "Three Days";
					break;
				//case "FourDays":
				//	FirstIcon.IsVisible = false;
				//	SecondIcon.IsVisible = false;
				//	ThirdIcon.IsVisible = false;
				//	FourthIcon.IsVisible = true;
				//	FifthIcon.IsVisible = false;
				//	SixthIcon.IsVisible = false;
				//	OfflineEpisodeSettings.Instance.Duration = "Four Days";
				//	break;
				case "OneWeek":
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = false;
					//FourthIcon.IsVisible = false;
					FifthIcon.IsVisible = true;
					//SixthIcon.IsVisible = false;
					OfflineEpisodeSettings.Instance.Duration = "One Week";
					break;
				//case "OneMonth":
				//	FirstIcon.IsVisible = false;
				//	SecondIcon.IsVisible = false;
				//	ThirdIcon.IsVisible = false;
				//	//FourthIcon.IsVisible = false;
				//	FifthIcon.IsVisible = false;
				//	//SixthIcon.IsVisible = true;
				//	OfflineEpisodeSettings.Instance.Duration = "One Month";
				//	break;
			}
			PlayerFeedAPI.UpdateOfflineEpisodeSettings();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			PlayerFeedAPI.CleanUpEpisodes();
		}
	}
}
