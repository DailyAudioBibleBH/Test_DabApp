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
            _database.CreateTable<dbUserData>();
            //Insert any settings preserved from prior version
            if (GlobalResources.SettingsToPreserve != null)
            {
                dbUserData userData = new dbUserData();
                //TODO: see if you can refactor this better using the dbUserData
                foreach (dbSettings s in GlobalResources.SettingsToPreserve)
                {
                    if (s.Key == "WpId")
                    {
                        userData.WpId = Convert.ToInt32(s.Value);
                    }
                    else if (s.Key == "FirstName")
                    {
                        userData.FirstName = s.Value;
                    }
                    else if (s.Key == "LastName")
                    {
                        userData.LastName = s.Value;
                    }
                    else if (s.Key == "Token")
                    {
                        userData.Token = s.Value;
                    }
                    else if (s.Key == "TokenCreation")
                    {
                        userData.TokenCreation = Convert.ToDateTime(s.Value);
                    }
                    else if (s.Key == "Email")
                    {
                        userData.Email = s.Value;
                    }
                    else if (s.Key == "UserRegistered")
                    {
                        userData.UserRegistered = Convert.ToDateTime(s.Value);
                    }
                    else
                    {
                        _database.InsertOrReplace(s);
                    }
                }
                _database.InsertOrReplace(userData);
            }
            _database.CreateTable<dbEpisodes>();
            _database.CreateTable<dbPlayerActions>();
            _database.CreateTable<dbBadges>();
            _database.CreateTable<dbUserBadgeProgress>();
            _database.CreateTable<dbChannels>();
            _database.CreateTable<dbEpisodeUserData>();
            _database.CreateTable<dbCreditCards>();
            _database.CreateTable<dbDataTransfers>();
            _database.CreateTable<dbCampaigns>();
            _database.CreateTable<dbPricingPlans>();
            _database.CreateTable<dbUserCampaigns>();
            _databaseInitiated = true;
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
