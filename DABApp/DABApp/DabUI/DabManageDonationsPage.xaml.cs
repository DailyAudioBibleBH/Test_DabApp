using System;
using System.Collections.Generic;
using System.Linq;
using DABApp.DabUI.BaseUI;
using DABApp.Helpers;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabManageDonationsPage : DabBaseContentPage
	{
		List<dbUserCampaigns> _donations;
		List<dbCampaigns> _campaigns;
		bool isInitialized = false;
		bool _fromLogin;
		object source;
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


		public DabManageDonationsPage(bool fromLogin = false)
		{
			InitializeComponent();
			_donations = new List<dbUserCampaigns>();
			_donations.Clear();
			if (Device.RuntimePlatform == "Android")
			{
                if (Device.Idiom == TargetIdiom.Phone)
                {
                    MessagingCenter.Send<string>("Remove", "Remove");
                }
                //else { NavigationPage.SetHasNavigationBar(this, false); }
			}
			else 
			{ 
				ToolbarItems.RemoveAt(ToolbarItems.Count - 1);
			}
			_campaigns = adb.Table<dbCampaigns>().Where(x => x.campaignStatus != "deleted").ToListAsync().Result;
			_fromLogin = fromLogin;
			if (GlobalResources.ShouldUseSplitScreen)
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			MessagingCenter.Subscribe<string>("Refresh", "Refresh", (sender) =>{
				OnAppearing();
			});
			if (_campaigns != null)
			{
				foreach (var cam in _campaigns)
				{
					dbCampaigns donation = adb.Table<dbCampaigns>().Where(x => x.campaignWpId == cam.campaignWpId).FirstOrDefaultAsync().Result;
					StackLayout layout = new StackLayout();
					StackLayout buttons = new StackLayout();
					buttons.Orientation = StackOrientation.Horizontal;
					Button btnInterval = new Button();
					Button once = new Button();
					Label cTitle = new Label();
					cTitle.Text = $"{donation.campaignTitle} - ${donation.campaignSuggestedRecurringDonation}/{donation.campaignDescription}Month";
					cTitle.Style = (Style)App.Current.Resources["playerLabelStyle"];
					cTitle.FontAttributes = FontAttributes.Bold;
					Label card = new Label();
					Label recurr = new Label();
					Label interval = new Label();
					btnInterval.Text = "Edit Donation";
					btnInterval.Clicked += OnRecurring;
					btnInterval.WidthRequest = 150;
					btnInterval.HeightRequest = 40;
					btnInterval.AutomationId = cam.campaignWpId.ToString();
					int wpid = GlobalResources.Instance.LoggedInUser.WpId;
					dbUserCampaigns pro = adb.Table<dbUserCampaigns>().Where(x => x.CampaignWpId == cam.campaignWpId && x.UserWpId == wpid && x.Status != "deleted").FirstOrDefaultAsync().Result;
					if (pro != null)
					{
						_donations.Add(pro);
						int cardId = Convert.ToInt32(pro.Source);
						dbCreditCards creditCard = adb.Table<dbCreditCards>().Where(x => x.cardWpId == cardId).FirstOrDefaultAsync().Result;
						dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.cardId == pro.Source).FirstOrDefaultAsync().Result;
						string currencyAmount = GlobalResources.ToCurrency(pro.Amount);
						btnInterval.Text = $"Edit Donation";
						cTitle.Text = $"{donation.campaignTitle} - ${currencyAmount}/{StringExtensions.ToTitleCase(pro.RecurringInterval)}";
						if (creditCard == null)
						{
							card.Text = $"Card not found";
						}
						else
						{
							card.Text = $"Card ending in {creditCard.cardLastFour}";

						}
						card.FontSize = 16;
						card.VerticalOptions = LayoutOptions.End;
						recurr.Text = $"Recurs: {Convert.ToDateTime(source.next).ToString("MM/dd/yyyy")}";
						recurr.FontSize = 14;
						recurr.VerticalOptions = LayoutOptions.Start;
						btnInterval.IsVisible = true;
						once.Text = "Make One Time Gift";
						once.HeightRequest = 40;
						buttons.Children.Add(btnInterval);
					}
					else
					{
						btnInterval.IsVisible = false;
						once.Text = "Give";
						once.HeightRequest = 40;
						once.HorizontalOptions = LayoutOptions.StartAndExpand;
					}
					once.WidthRequest = 150;
					once.AutomationId = cam.campaignWpId.ToString();
					once.Clicked += OnGive;
					buttons.Children.Add(btnInterval);
					buttons.Children.Add(once);
					layout.Children.Add(cTitle);
					layout.Children.Add(card);
					layout.Children.Add(recurr);
					layout.Children.Add(buttons);
					layout.BackgroundColor = (Color)App.Current.Resources["InputBackgroundColor"];
					layout.Padding = 10;
					layout.Spacing = 10;
					layout.AutomationId = cam.campaignWpId.ToString();
					Container.Children.Insert(1, layout);
					
					
				}
			}
		}

		async void OnHistory(object o, EventArgs e) 
		{

			await Navigation.PushAsync(new DabDonationHistoryPage());
			
			isInitialized = false;
		}

		async void OnRecurring(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
			Button chosen = (Button)o;
			List<dbCreditCards> cards = AuthenticationAPI.GetWallet();
			var a = _donations;
			var campaign = _donations.Single(x => x.CampaignWpId.ToString() == chosen.AutomationId);
			await Navigation.PushAsync(new DabEditRecurringDonationPage(campaign));
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
		}

		async void OnGive(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
			Button chosen = (Button)o;
			//TODO: check this
			var url = await PlayerFeedAPI.PostDonationAccessToken(chosen.AutomationId);
			if (!url.Contains("Error"))
			{
				DependencyService.Get<IRivets>().NavigateTo(url);
			}
			else 
			{
				await DisplayAlert("An Error has occured.", url, "OK");
			}
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			if (_fromLogin) {
				Navigation.InsertPageBefore(new DabChannelsPage(), this);
			}
			if (Device.Idiom == TargetIdiom.Phone)
			{
				MessagingCenter.Send<string>("Remove", "Remove");
			}
			if (isInitialized)
			{
				source = new object();
				DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));
				_donations.Clear();
				if (_campaigns != null)
				{
					foreach (var cam in _campaigns)
					{
						dbCampaigns donation = adb.Table<dbCampaigns>().Where(x => x.campaignWpId == cam.campaignWpId).FirstOrDefaultAsync().Result;

						StackLayout donContainer = (StackLayout)Container.Children.SingleOrDefault(x => x.AutomationId == cam.campaignWpId.ToString());
						var Labels = donContainer.Children.Where(x => x.GetType() == typeof(Label)).Select(x => (Label)x).ToList();
						var ButtonContainer = donContainer.Children.SingleOrDefault(x => x.GetType() == typeof(StackLayout)) as StackLayout;
						var Buttons = ButtonContainer.Children.Where(x => x.GetType() == typeof(Button)).Select(x => (Button)x).ToList();
						dbUserCampaigns pro = adb.Table<dbUserCampaigns>().Where(x => x.CampaignWpId == cam.campaignWpId && x.Status != "deleted").FirstOrDefaultAsync().Result;
						if (pro != null)
						{
							_donations.Add(pro);
							int cardId = Convert.ToInt32(pro.Source);
							dbCreditCards creditCard = adb.Table<dbCreditCards>().Where(x => x.cardWpId == cardId).FirstOrDefaultAsync().Result;
							dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.cardId == pro.Source).FirstOrDefaultAsync().Result;
							string currencyAmount = GlobalResources.ToCurrency(pro.Amount);

							Labels[0].Text = $"{donation.campaignTitle} - ${currencyAmount}/{StringExtensions.ToTitleCase(pro.RecurringInterval)}";
							Labels[1].Text = $"Card ending in {creditCard.cardLastFour}";
							Labels[2].Text = $"Recurs: {Convert.ToDateTime(source.next).ToString("MM/dd/yyyy")}";
							Labels[1].IsVisible = true;
							Labels[2].IsVisible = true;
							Labels[1].FontSize = 14;
							Labels[2].FontSize = 14;
							Buttons[0].IsVisible = true;
							Buttons[1].Text = "One-time gift";
							Buttons[0].Text = $"Edit Donation";
						}
						else
						{
							Labels[1].Text = null;
							Labels[2].Text = null;
							Buttons[0].IsVisible = false;
							Buttons[1].Text = "Give";
							Buttons[1].HeightRequest = 40;
						}
					}
				}
				else
				{
					await DisplayAlert("Unable to retrieve Donation information", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
				}
				DabUserInteractionEvents.WaitStopped(source, new EventArgs());
			}
			isInitialized = true;
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			MessagingCenter.Send<string>("Show", "Show");
		}
	}
}
