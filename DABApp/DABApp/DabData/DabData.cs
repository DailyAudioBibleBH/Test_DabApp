using System;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public class DabData
	{
		public static readonly bool ResetDatabaseOnStart = false;

		static SQLiteConnection _database;
		static SQLiteAsyncConnection _aDatabase;
		static bool _databaseInitiated = false;
		public static SQLiteConnection database { 
			get {
				if (!_databaseInitiated) {
					initDatabase();
				}
				return _database;
			}
		}

		public static SQLiteAsyncConnection AsyncDatabase { 
			get {
				if (!_databaseInitiated) {
					initDatabase();
				}
				return _aDatabase;
			}
		}

		static void initDatabase() {
			_database = DependencyService.Get<ISQLite>().GetConnection(ResetDatabaseOnStart);
			_aDatabase = DependencyService.Get<ISQLite>().GetAsyncConnection(ResetDatabaseOnStart);
			_database.CreateTable<dbSettings>();
			_database.CreateTable<dbEpisodes>();
			_database.CreateTable<dbPlayerActions>();
		}
	}
}
