using System;
using System.Collections.Generic;
using System.Linq;
using DABApp.DabSockets;
using Newtonsoft.Json;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabUpdateAddressPage : DabBaseContentPage
	{
		bool isShipping;
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        public Dictionary<string, object> stateDictionary;
		public Dictionary<string, string> labelDictionary;
		public Dictionary<string, string> countryDictionary;
        public Dictionary<string, string> currentStateDictionary;
		public string CountrySettings;
		public string LabelSettings;
		public string StateSettings;

        public DabUpdateAddressPage(DabGraphQlAddress address, Dictionary<string, string> countries, bool IsShipping)
		{
			InitializeComponent();
			if (GlobalResources.ShouldUseSplitScreen)
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			BindingContext = address;
			isShipping = IsShipping;
			if (IsShipping) {
				EmailAndPhone.IsVisible = false;
				Title.Text = "Shipping Address";
			}

			//Finding and then storing all available country, labels, and states
			CountrySettings = dbSettings.GetSetting("Country", "");
			LabelSettings = dbSettings.GetSetting("Labels", "");
			StateSettings = dbSettings.GetSetting("States", "");

			countryDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(CountrySettings);
			labelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(LabelSettings);
			stateDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(StateSettings);
			Country.ItemsSource = countries.Values.ToList();

			//If user already has an address tied to them, grab and bind appropriate data
            if (address.country != null)
            {
				Country.SelectedItem = countryDictionary.FirstOrDefault(x => x.Value == address.country).Value;

				object newCountry = address.country;
				string countryCode = countryDictionary.Where(x => x.Value == newCountry.ToString()).ToList().FirstOrDefault().Key;
				object countryStates = stateDictionary.Where(x => x.Key == countryCode).ToList().FirstOrDefault().Value;


				if (countryStates != null)
                {
					currentStateDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(countryStates.ToString());
					Regions.ItemsSource = currentStateDictionary.Values.ToList();
                    if (address.state != null)
                    {
						Regions.SelectedItem = currentStateDictionary.FirstOrDefault(x => x.Key == address.state).Value;
                    }
				}
                else
                {
					_Region.IsVisible = false;
                }
			}
            else
            {
				//Default selected country to USA if no country tied to user
				Country.SelectedItem = countryDictionary.FirstOrDefault(x => x.Value == "United States (US)").Value;
			}

			//Country.ItemDisplayBinding = new Binding("countryName");
			//if (string.IsNullOrEmpty(address.country))
			//{
			//	Country.SelectedItem = countries.Single(x => x.countryCode == "US");
			//}
			//else
			//{
			//	Country.SelectedItem = countries.Single(x => x.countryCode == address.country);
			//}
			//Country setCountry = ((Country)Country.SelectedItem);
			//if (setCountry.regions.Length == 0)
			//{
			//	_Region.IsVisible = false;
			//}
			//else _Region.IsVisible = true;
			//Regions.ItemsSource = setCountry.regions;
			//Regions.ItemDisplayBinding = new Binding("regionName");
			//RegionLabel.Text = setCountry.regionLabel;
			//Regions.SelectedItem = setCountry.regions.SingleOrDefault(x => x.regionCode == address.state);
			//CodeLabel.Text = setCountry.postalCodeLabel;
		}

		async void OnSave(object o, EventArgs e) 
		{
			if (Validation())
			{
				GlobalResources.WaitStart();
				var update = new Address();
				update.first_name = FirstName.Text;
				update.last_name = LastName.Text;
				update.company = CompanyName.Text;
				update.email = GlobalResources.GetUserEmail();
				update.phone = Phone.Text;
				update.address_1 = Address1.Text;
				update.address_2 = Address2.Text;
				update.city = City.Text;
				update.postcode = Code.Text;
				update.country = Country.SelectedItem.ToString();
				if (Regions.SelectedItem != null)
				{
					update.state = currentStateDictionary.FirstOrDefault(x => x.Value == Regions.SelectedItem.ToString()).Key;
				}
				if (isShipping)
					update.type = "shipping";
				else
					update.type = "billing";

				//Send updated address to graph ql and wait for response
				var result = await Service.DabService.UpdateUserAddress(update);
				if (result.Success == false) throw new Exception(result.ErrorMessage);
                else
                {
					await DisplayAlert("Success", "Address successfully updated", "OK");
					await Navigation.PopAsync();
				}
				
				GlobalResources.WaitStop();
			}
		}

		void OnCountrySelected(object o, EventArgs e)
		{
			//Select country and find appropriate regions list for country
			object newCountry = Country.SelectedItem;
			string countryCode = countryDictionary.Where(x => x.Value == newCountry.ToString()).ToList().FirstOrDefault().Key;
			object countryStates = stateDictionary.Where(x => x.Key == countryCode).ToList().FirstOrDefault().Value;
            if (countryStates != null)
            {
				currentStateDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(countryStates.ToString());
				Regions.ItemsSource = currentStateDictionary.Values.ToList();
				_Region.IsVisible = true;
			}
            else
            {
				Regions.SelectedItem = null;
				_Region.IsVisible = false;
            }
			RegionLabel.Text = labelDictionary.Where(x => x.Key == countryCode).ToList().FirstOrDefault().Value;

            //if (stateDictionary.Where(x => x.Key == countryCode.Value))
            //if (newCountry.ToString().Length == 0)
            //{
            //	_Region.IsVisible = false;
            //}
            //else _Region.IsVisible = true;

            //if (countryStates.Length == 0)
            //{
            //    _Region.IsVisible = false;
            //}
            //else _Region.IsVisible = true;
            //Regions.ItemsSource = newCountry.regions;
            //RegionLabel.Text = newCountry.regionLabel;
            //CodeLabel.Text = newCountry.postalCodeLabel;
        }

		bool Validation() 
		{
			bool result = true;
			if (string.IsNullOrEmpty(FirstName.Text)) {
				result = false;
				FirstNameWarning.IsVisible = true;
			}
			if (string.IsNullOrEmpty(LastName.Text)) 
			{
				result = false;
				LastNameWarning.IsVisible = true;
			}
			if (Country.SelectedItem != null)
			{
				object selected = Country.SelectedItem;
				if (selected.ToString() == "United States (US)")
				{
					if (string.IsNullOrEmpty(Address1.Text))
					{
						result = false;
						AddressWarning.IsVisible = true;
					}
					if (string.IsNullOrEmpty(City.Text))
					{
						result = false;
						CityWarning.IsVisible = true;
					}
					if (Regions.SelectedItem == null)
					{
						result = false;
						RegionWarning.IsVisible = true;
					}
					if (string.IsNullOrEmpty(Code.Text)) 
					{
						result = false;
						CodeWarning.IsVisible = true;
					}
				}
			}
			else 
			{
				CountryWarning.IsVisible = true;
				result = false;
			}
			if (result) 
			{
				FirstNameWarning.IsVisible = false;
				LastNameWarning.IsVisible = false;
				CountryWarning.IsVisible = false;
				AddressWarning.IsVisible = false;
				CityWarning.IsVisible = false;
				CodeWarning.IsVisible = false;
				RegionWarning.IsVisible = false;
			}
			return result;
		}
	}
}
