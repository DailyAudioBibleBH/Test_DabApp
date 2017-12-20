using System;
using System.ComponentModel;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public class DabData
	{
        public static readonly bool ResetDatabaseOnStart = false; //Set to true to clear the database at every launch

		static SQLiteConnection _database;
		static SQLiteAsyncConnection _AsyncDatabase;
		static bool _databaseInitiated = false;
		public static SQLiteConnection database
		{
			get
			{
				if (!_databaseInitiated)
				{
					initDatabase();
				}
				return _database;
			}
		}

		public static SQLiteAsyncConnection AsyncDatabase
		{
			get
			{
				if (!_databaseInitiated)
				{
					initDatabase();
				}
				return _AsyncDatabase;
			}
		}

		static void initDatabase()
		{
			_database = DependencyService.Get<ISQLite>().GetConnection(ResetDatabaseOnStart);
			_AsyncDatabase = DependencyService.Get<ISQLite>().GetAsyncConnection(ResetDatabaseOnStart);
			_database.BusyTimeout = TimeSpan.FromSeconds(60);
			_AsyncDatabase.GetConnection().BusyTimeout = TimeSpan.FromSeconds(60);
			_database.ExecuteScalar<string>("PRAGMA journal_mode=WAL");//Enabling Write Ahead Log instead of rollback journal.
			_database.CreateTable<dbSettings>();
			_database.CreateTable<dbEpisodes>();
			_database.CreateTable<dbPlayerActions>();
		}

		public static void ResetDatabases()
		{
			_database.Dispose();
			_AsyncDatabase.GetConnection().Dispose();
			_database = DependencyService.Get<ISQLite>().GetConnection(ResetDatabaseOnStart);
			_AsyncDatabase = DependencyService.Get<ISQLite>().GetAsyncConnection(ResetDatabaseOnStart);
			_database.BusyTimeout = TimeSpan.FromSeconds(60);
			_AsyncDatabase.GetConnection().BusyTimeout = TimeSpan.FromSeconds(60);
			_database.ExecuteScalar<string>("PRAGMA journal_mode=WAL");
			NotifyStaticPropertyChanged("Database");
			NotifyStaticPropertyChanged("AsyncDatabase");
		}

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

		private static void NotifyStaticPropertyChanged(string propertyName)
		{
			StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
	}
}
