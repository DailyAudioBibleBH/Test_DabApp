using System;
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
		public string user_password1 { get; set;}
		public string user_password2 { get; set;}
		public EditProfileInfo(string email, string firstName, string lastName, string currentPassword, string password1, string password2)
		{
			user_email = email;
			user_first_name = firstName;
			user_last_name = lastName;
			user_password_current = currentPassword;
			user_password1 = password1;
			user_password2 = password2;
		}
	}

	public class ProfileInfo { 
		public string email { get; set;}
		public string first_Name { get; set;}
		public string last_Name { get; set;}
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
	}

	public class APIAddresses
	{
		public Address billing { get; set; }
		public Address shipping { get; set; }
	}

	public class Card
	{
		public string processor { get; set; }
		public string brand { get; set; }
		public string last4 { get; set; }
		public int exp_month { get; set; }
		public int exp_year { get; set; }
		public string id { get; set; }
		public bool Default { get; set;}
		public string fullNumber { get; set;}
		public string cvc { get; set;}
	}

	public class StripeContainer { 
		public string card_token { get; set;} 
	}

	//public class StripeToken { 
	//	public StripeCard Card { get; set;}
	//	public string Id { get; set; }
	//	public bool LiveMode { get; set;}
	//	public bool Used { get; set;}
	//}

	//public class StripeCard { 
	//	public string CVC { get; set;}
	//	public int ExpiryMonth { get; set;}
	//	public int ExpiryYear { get; set;}
	//	public string Id { get; set;}
	//	public string Number { get; set; }
	//}
}
