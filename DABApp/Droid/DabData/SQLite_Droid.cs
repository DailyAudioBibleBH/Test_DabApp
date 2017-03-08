using System;
using Android.Content.Res;
using System.IO;
using SQLite;
using DABApp.Droid;
using Xamarin.Forms;

[assembly:Dependency(typeof(SQLite_Droid))]
namespace DABApp.Droid
{
	public class SQLite_Droid: ISQLite
	{
		public static AssetManager Assets { get; set; }

		private bool _initiated = false;

		public SQLiteConnection GetConnection(bool ResetDatabaseOnStart)
		{
			//Build the path for storing the Android database
			var filename = "GbrSQLite.db3";
			string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
			var path = Path.Combine(folder, filename);

			if (!_initiated)
			{
				//do things just once while the app is running
				_initiated = true;
				//Reset the file if requested
				if (ResetDatabaseOnStart)
				{
					if (File.Exists(path))
					{
						File.Delete(path);
					}
				}
			}

			var connection = new SQLiteConnection(path);
			return connection;
		}
	}
}
