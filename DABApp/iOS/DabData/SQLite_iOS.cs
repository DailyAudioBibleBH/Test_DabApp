using System;
using Xamarin.Forms;
using DABApp.iOS;
using DABApp;
using System.IO;
using SQLite;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

[assembly: Dependency(typeof(SQLite_iOS))]
namespace DABApp.iOS
{
    public class SQLite_iOS : ISQLite
    {
        private static bool _initiated = false;

        public SQLiteConnection GetConnection(bool ResetDatabaseOnStart)
        {
            // Create the connection
            var conn = new SQLiteConnection(GetDatabasePath(ResetDatabaseOnStart));
            // Return the database connection
            return conn;
        }

        public SQLiteAsyncConnection GetAsyncConnection(bool ResetDatabaseOnStart)
        {
            var connection = new SQLiteAsyncConnection(GetDatabasePath(ResetDatabaseOnStart));
            return connection;
        }

        private string GetDatabasePath(bool ResetDatabaseOnStart)
        {
            bool hasUserTable = false;
            List<dbUserData> userSettings;
            dbUserData user = new dbUserData();
            //Build the path for storing the Android database
            //var sqliteFilename = "database.db3";
            var sqliteFilename = $"database.{GlobalResources.DBVersion}.db3";
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

                //Cleanup old database files (with names other than the one we're using)
                DirectoryInfo dir = new DirectoryInfo(libraryPath);
                foreach (FileInfo fil in dir.GetFiles(("*.db3")))
                {
                    if (fil.Name != sqliteFilename)
                    {
                        try
                        {

                            /* Extract any information we need from the prior database here before deleting it */
                            switch (fil.Name)
                            {
                                case "database.20191210-AddedUserEpisodeMeta-b.db3": //production 1.1.14 - treat as default
                                default:
                                    {
                                        var cn = new SQLiteConnection(fil.FullName);
                                        var settings = cn.Table<dbSettings>().ToList();
                                        try
                                        {
                                            //depending on app version user may not have this table
                                            userSettings = cn.Table<dbUserData>().ToList();
                                            user = cn.Table<dbUserData>().ToList().FirstOrDefault();
                                            hasUserTable = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            hasUserTable = false;
                                        }
                                        
                                        cn.Close();
                                        if (GlobalResources.SettingsToPreserve == null)
                                        {
                                            //if user table info is empty then nothing to save either way 
                                            //or updating from previous version and user data is already in
                                            //dbSettings so make sure not to overright.
                                            if (hasUserTable)
                                            {
                                                if (!string.IsNullOrEmpty(user.Email))
                                                {
                                                    //preserve user settings
                                                    settings.Add(new dbSettings() { Key = "WpId", Value = user.WpId.ToString() });
                                                    settings.Add(new dbSettings() { Key = "FirstName", Value = user.FirstName });
                                                    settings.Add(new dbSettings() { Key = "LastName", Value = user.LastName });
                                                    settings.Add(new dbSettings() { Key = "Token", Value = user.Token });
                                                    settings.Add(new dbSettings() { Key = "TokenCreation", Value = user.TokenCreation.ToString() });
                                                    settings.Add(new dbSettings() { Key = "Email", Value = user.Email });
                                                    settings.Add(new dbSettings() { Key = "UserRegistered", Value = user.UserRegistered.ToString() });
                                                }
                                            }
                                            

                                            GlobalResources.SettingsToPreserve = new List<dbSettings>();

                                            //Loop through settings, choose some to preserve (add them to a list in global settings that will add them back if needed later)
                                            foreach (dbSettings s in settings)
                                            {
                                                try
                                                {
                                                    switch (s.Key)
                                                    {
                                                        case "ContentJSON":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "data":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "AvailableOffline":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "OfflineEpisodes":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "Token": //
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "TokenExpiration":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            //Add a presumed TokenCreation settings
                                                            DateTime expires = DateTime.Parse(s.Value);
                                                            if (expires >= DateTime.Now.AddHours(1)) //Create a usable token creation date as long as the token is good for the next hour
                                                            {
                                                                DateTime created = expires.AddHours(-1); //Will force a token exchange soon.
                                                                GlobalResources.SettingsToPreserve.Add(new dbSettings() { Key = "TokenCreation", Value = created.ToString() }); //this may NOT be the original token creation date but we have to have something.
                                                            }
                                                            break;
                                                        case "TokenCreation":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "Email":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "FirstName":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "LastName":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "Avatar":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "WpId":
                                                            GlobalResources.SettingsToPreserve.Add(s);
                                                            break;
                                                        case "ActionDate": //IGNORE THIS ONE!
                                                            break; //IGNORE THIS!
                                                        default:
                                                            break;
                                                    }
                                                }
                                                catch (Exception exSetting)
                                                {
                                                    //Error saving a setting... move along.
                                                    Debug.WriteLine($"Setting Preservation Error during {s.Key}: {exSetting.ToString()}");
                                                }
                                            }
                                        }

                                        break;

                                    }

                            }

                        }
                        catch (Exception exDatabaseFileException)
                        {
                            Debug.WriteLine($"Previous database processing error during {fil.Name}: {exDatabaseFileException.ToString()}");
                        }



                        fil.Delete();
                    }
                }
            }

            return path;

        }
    }
}
