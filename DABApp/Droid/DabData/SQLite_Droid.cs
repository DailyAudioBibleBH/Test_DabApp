﻿using System;
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
			//var filename = "DabSQLite.db3";
            var filename = $"database.{GlobalResources.DBVersion}.db3";

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

                //Cleanup old database files (with names other than the one we're using)
                DirectoryInfo dir = new DirectoryInfo(libraryPath);
                foreach (FileInfo fil in dir.GetFiles(("*.db3")))
                {
                    if (fil.Name != filename)
                    {
                        fil.Delete();
                    }
                }

			}

			var connection = new SQLiteConnection(path);
			return connection;
		}

		public SQLiteAsyncConnection GetAsyncConnection(bool ResetDatabaseOnStart)
		{
			//Build the path for storing the Android database
			//var filename = "DabSQLite.db3";
            var filename = $"database.{GlobalResources.DBVersion}.db3";
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

                //Cleanup old database files (with names other than the one we're using)
                DirectoryInfo dir = new DirectoryInfo(libraryPath);
                foreach (FileInfo fil in dir.GetFiles(("*.db3")))
                {
                    if (fil.Name != filename)
                    {
                        fil.Delete();
                    }
                }

			}

			var connection = new SQLiteAsyncConnection(path);
			return connection;
		}
	}
}
