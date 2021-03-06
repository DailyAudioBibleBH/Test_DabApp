using System;
using System.Collections.Generic;

namespace DABApp
{
	public class APIToken
	{
		public string value { get; set; }
		public string expires { get; set; }
		public string user_email { get; set; }
		public string user_first_name { get; set; }
		public string user_last_name { get; set; }
		public string user_avatar { get; set; }
	}

	public class APITokenContainer
	{
		public APIToken token { get; set; }
		public string code { get; set; }
        public string message { get; set; }
        //public List<string> message { get; set; } //no longer a list?
	}

	public class LoginInfo
	{
		public string user_email { get; set; }
		public string user_password { get; set; }
		public LoginInfo(string email, string password)
		{
			user_email = email;
			user_password = password;
		}
	}

	public class SignUpInfo
	{
		public string user_email { get; set; }
		public string user_first_name { get; set; }
		public string user_last_name { get; set; }
		public string user_password { get; set; }
		public SignUpInfo(string email, string firstName, string lastName, string password)
		{
			user_email = email;
			user_first_name = firstName;
			user_last_name = lastName;
			user_password = password;
		}
	}

	public class ResetEmailInfo
	{
		public string user_email { get; set; }
		public ResetEmailInfo(string email)
		{
			user_email = email;
		}
	}

	public class LogOutInfo
	{
		public string auth_token { get; set; }
		public LogOutInfo(string token)
		{
			auth_token = token;
		}
	}

	public class EditProfileInfo
	{
		public string user_email { get; set; }
		public string user_first_name { get; set; }
		public string user_last_name { get; set; }
		public string user_password_current { get; set; }
		public string user_password1 { get; set; }
		public string user_password2 { get; set; }
		public EditProfileInfo(string email, string firstName, string lastName)
		{
			user_email = email;
			user_first_name = firstName;
			user_last_name = lastName;
		}
	}

	public class ProfileInfo
	{
		public string email { get; set; }
		public string first_Name { get; set; }
		public string last_Name { get; set; }
	}


	public class Address
	{
		public string first_name { get; set; }
		public string last_name { get; set; }
		public string company { get; set; }
		public string email { get; set; }
		public string phone { get; set; }
		public string address_1 { get; set; }
		public string address_2 { get; set; }
		public string city { get; set; }
		public string state { get; set; }
		public string postcode { get; set; }
		public string country { get; set; }
		public string type { get; set; }
	}

	public class APIAddresses
	{
		public Address billing { get; set; }
		public Address shipping { get; set; }
	}

	public class Country
	{
		public string countryName { get; set; }
		public string countryCode { get; set; }
		public string cityLabel { get; set; }
		public bool postalCodeBeforeCity { get; set; }
		public string postalCodeLabel { get; set; }
		public string postalCodeKeyboard { get; set; }
		public string regionLabel { get; set; }
		public Region[] regions { get; set; }
		public bool stateRequired { get; set; }
	}

	public class Region
	{
		public string regionName { get; set; }
		public string regionCode { get; set; }
	}

	public class Card
	{
		public string processor { get; set; }
		public string brand { get; set; }
		public string last4 { get; set; }
		public int exp_month { get; set; }
		public int exp_year { get; set; }
		public string id { get; set; }
		public bool Default { get; set; }
		public string fullNumber { get; set; }
		public string cvc { get; set; }
	}

	public class StripeContainer
	{
		public string card_token { get; set; }
	}

	public class DonationContainer
	{
		public Donation[] data { get; set; }
	}

	public class Donation
	{
		public int id { get; set; }
		public string link { get; set; }
		public string name { get; set; }
		public string description { get; set; }
		public bool singleDonationIsActive { get; set; }
		public string suggestedSingleDonation { get; set; }
		public bool recurringDonationIsActive { get; set; }
		public string suggestedRecurringDonation { get; set; }
		public List<string> recurringIntervalOptions { get; set; }
		public Pro pro { get; set; }
	}

	public class Pro
	{
		public string status { get; set; }
		public double amount { get; set; }
		public string card_last_four { get; set; }
		public int card_exp_month { get; set; }
		public int card_exp_year { get; set; }
		public string card_id { get; set; }
		public string next { get; set; }
		public string interval { get; set; }
		public string id { get; set; }
		public string processor { get; set; }
	}

	public class putDonation
	{
		public int campaign_number { get; set; }
		public string card_id { get; set; }
		public string amount { get; set; }
        public string donation_type { get; set; }
        public long next_date_timestamp { get; set; }
		public putDonation(int campaign, string card, string Amount, string DonationType, long date)
		{
			campaign_number = campaign;
			card_id = card;
			amount = Amount;
			donation_type = DonationType;
			next_date_timestamp = date;
		}
	}

	public class postDonation
	{
		public int campaign_number { get; set; }
		public string card_id { get; set; }
		public string amount { get; set; }
		public long next_date_timestamp { get; set; }
		public string country { get; set; }
		public string address_1 { get; set; }
		public string address_2 { get; set; }
		public string city { get; set; }
		public string state { get; set; }
		public postDonation(int campaign, string card, string Amount, long date, string Country, string address1 = null, string address2 = null, string City = null, string State = null)
		{
			campaign_number = campaign;
			card_id = card;
			amount = Amount;
			next_date_timestamp = date;
			country = Country;
			address_1 = address1;
			address_2 = address2;
			city = City;
			state = State;
		}
	}

	public class DonationRecord
	{
		public string date { get; set; }
		public string campaignName { get; set; }
		public string donationType { get; set; }
		public string grossAmount { get; set; }
		public string currency { get; set; }
	}

	public class DonationTokenContainer
	{
		public APIToken token { get; set; }
		public string csrf_dab_token { get; set; }
		public string campaign_id { get; set;}
		public string redirect_url { get; set; } = "dab://";
	}

	public class GetTokenContainer
	{
		public GetToken csrf { get; set; }
	}

	public class GetToken
	{
		public string token_name { get; set; }
		public string token_value { get; set; }
	}

	public class RequestedUrl
	{
		public string url { get; set;}
	}

	public class APIError { 
		public string code { get; set;}
		public string message { get; set;}
	}
}
