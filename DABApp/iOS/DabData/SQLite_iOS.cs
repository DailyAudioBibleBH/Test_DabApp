﻿using System;
using Xamarin.Forms;
using DABApp.iOS;
using DABApp;
using System.IO;
using SQLite;

[assembly:Dependency(typeof(SQLite_iOS))]
namespace DABApp.iOS
{
	public class SQLite_iOS: ISQLite
	{
		private static bool _initiated = false;

		public SQLiteConnection GetConnection(bool ResetDatabaseOnStart)
		{
			//Build the path for storing the iOS database
			var sqliteFilename = "database.db3";
			string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
			string libraryPath = Path.Combine(documentsPath, "..", "Library"); // Library folder
			var path = Path.Combine(libraryPath, sqliteFilename);

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

			// Create the connection
			var conn = new SQLiteConnection(path);
			// Return the database connection
			return conn;
		}
	}
}
