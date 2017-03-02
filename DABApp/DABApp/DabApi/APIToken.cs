using System;
namespace DABApp
{
	public class APIToken
	{
		public string value { get; set;}
		public string expires { get; set;}
		public string user_email { get; set;}
		public string user_first_name { get; set;}
		public string user_last_name { get; set;}
		public string user_avatar { get; set;}
	}

	public class APITokenContainer 
	{
		public APIToken token { get; set;}
		public string code { get; set; }
		public string message { get; set; }
	}

	public class LoginInfo { 
		public string user_email { get; set;}
		public string user_password { get; set;}
		public LoginInfo(string email, string password) {
			user_email = email;
			user_password = password;
		}
	}

	public class SignUpInfo { 
		public string user_email { get; set;}
		public string user_first_name { get; set;}
		public string user_last_name { get; set;}
		public string user_password { get; set;}
		public SignUpInfo(string email, string firstName, string lastName, string password) {
			user_email = email;
			user_first_name = firstName;
			user_last_name = lastName;
			user_password = password;
		}
	}
}
