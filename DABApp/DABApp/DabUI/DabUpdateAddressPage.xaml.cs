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
			Country.ItemsSource = countries.Values.ToList();

			dbSettings CountrySettings = adb.Table<dbSettings>().Where(x => x.Key == "Country").FirstOrDefaultAsync().Result;
			dbSettings LabelSettings = adb.Table<dbSettings>().Where(x => x.Key == "Labels").FirstOrDefaultAsync().Result;
			dbSettings StateSettings = adb.Table<dbSettings>().Where(x => x.Key == "States").FirstOrDefaultAsync().Result;

			countryDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(CountrySettings.Value);
			labelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(LabelSettings.Value);
			stateDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(StateSettings.Value);

			var breakpoint = "";

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
				update.country = ((Country)Country.SelectedItem).countryCode;
				if (Regions.SelectedItem != null)
				{
					update.state = ((Region)Regions.SelectedItem).regionCode;
				}
				if (isShipping)
					update.type = "shipping";
				else
					update.type = "billing";

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

		void OnCountrySelected(object o, EventArgs e) {
			object newCountry = Country.SelectedItem;
			var countryCode = countryDictionary.Where(x => x.Value == newCountry.ToString()).ToList().FirstOrDefault().Key;
			//var countryStates = stateDictionary.Where(x => x.Key == countryCode).ToList().FirstOrDefault().Value;

			try
			{
				var countryStates = stateDictionary.Where(x => x.Key == "US").ToList().FirstOrDefault().Value;
				Dictionary<string, string> stateDictionary2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(countryStates.ToString());

			}
			catch (Exception ex)
            {

            }

			//if (countryStates != null)


			var breakpoint = "";
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
				Country selected = (Country)Country.SelectedItem;
				if (selected.countryCode == "US")
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
