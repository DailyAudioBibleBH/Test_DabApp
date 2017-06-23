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
}
