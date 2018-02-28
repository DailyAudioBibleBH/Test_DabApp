using System;
using SlideOverKit;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using FFImageLoading.Forms;
using System.Threading.Tasks;
using Plugin.Connectivity;
using System.Linq;

namespace DABApp
{
	public class DabBaseContentPage : MenuContainerPage
	{
        //public ActivityIndicator activity { get; set;}
        //public StackLayout activityHolder { get; set;}
        bool giving;

		public DabBaseContentPage()
		{
			//Default Page properties
			//this.Padding = new Thickness(10, 10); //Add some padding around all page controls
			Title = "DAILY AUDIO BIBLE";
			//Control template (adds the player bar)
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplate"];
			RelativeLayout container = new RelativeLayout();
			ControlTemplate = playerBarTemplate;
            On<Xamarin.Forms.PlatformConfiguration.iOS>().SetUseSafeArea(true);
			//activityHolder = new StackLayout()
			//
			//	Opacity = 0.5,
			//	BackgroundColor = Color.Gray,
			//	IsVisible = true
			//};
			//activity = new ActivityIndicator()
			//{
			//	IsRunning = true,
			//	IsVisible = true,
			//	VerticalOptions = LayoutOptions.CenterAndExpand,
			//	HorizontalOptions = LayoutOptions.CenterAndExpand,
			//	Color = Color.White
			//};
			//container.Children.Add(activityHolder, Constraint.RelativeToParent((parent) => { return parent.Width; }), Constraint.RelativeToParent((parent) => { return parent.Height; }));
			//container.Children.Add(activity, Constraint.RelativeToParent((parent) => { return parent.Width; }), Constraint.RelativeToParent((parent) => { return parent.Height; }));

			//ContentView view = new ContentView()
			//{
			//	Content = container,
			//	ControlTemplate = playerBarTemplate
			//};
			//Content = view;

			//Navigation properties
			Xamarin.Forms.NavigationPage.SetBackButtonTitle(this, "");

			//Slide Menu
			this.SlideMenu = new DabMenuView();
            if (Device.RuntimePlatform == "iOS")
            {
                //Menu Button
                var menuButton = new ToolbarItem();
                //menuButton.Text = "menu";
                menuButton.Priority = 1; //priority 1 causes it to be moved to the left by the platform specific navigation renderer
                menuButton.Icon = "ic_menu_white.png";
                menuButton.Clicked += (sender, e) =>
                {
                    this.ShowMenu();
                };
                this.ToolbarItems.Add(menuButton);

                //Give button on the right (priority 1)
                var giveButton = new ToolbarItem();
                giveButton.Text = "Give";
                //giveButton.Icon = "ic_attach_money_white.png";
                giveButton.Priority = 0; //default
                giveButton.Clicked += OnGive;
                this.ToolbarItems.Add(giveButton);
            }
		}

		async void OnGive(object o, EventArgs e) 
		{
                if (!giving)
                {
                    giving = true;
                    if (GuestStatus.Current.IsGuestLogin)
                    {
                        if (CrossConnectivity.Current.IsConnected)
                        {
                            var choice = await DisplayAlert("Login Required", "You must be logged in to access this service. Would you like to log in?", "Yes", "No");
                            if (choice)
                            {
                                var nav = new Xamarin.Forms.NavigationPage(new DabLoginPage(false, true));
                                nav.SetValue(Xamarin.Forms.NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                                await Navigation.PushModalAsync(nav);
                            }
                        }
                        else await DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
                    }
                    else
                    {
                        var dons = await AuthenticationAPI.GetDonations();
                        if (dons != null)
                        {
                            if (dons.Length == 1)
                            {
                                var url = await PlayerFeedAPI.PostDonationAccessToken();
                                if (url.StartsWith("http"))
                                {
                                    DependencyService.Get<IRivets>().NavigateTo(url);
                                }
                                else
                                {
                                    await DisplayAlert("Error", url, "OK");
                                }
                            }
                            else await Navigation.PushAsync(new DabManageDonationsPage(dons));
                        }
                        else await DisplayAlert("Unable to get Donation information.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
                    }
                    giving = false;
                }
		}

        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<string>("Menu", "Menu");
            MessagingCenter.Unsubscribe<string>("Give", "Give");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<string>("Menu", "Menu");
            MessagingCenter.Unsubscribe<string>("Give", "Give");
        }

        protected override void OnAppearing()
        { 
            base.OnAppearing();
            if (Device.RuntimePlatform == "Android")
            {
                MessagingCenter.Subscribe<string>("Menu", "Menu", (sender) =>
                {
                    if (Navigation.NavigationStack.Count() > 0 && Navigation.NavigationStack.Last() == this)
                    {
                        this.ShowMenu();
                    }
                });
                MessagingCenter.Subscribe<string>("Give", "Give", (sender) => { OnGive(sender, new EventArgs()); });
            }
        }
    }
}

