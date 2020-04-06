using System;
using Android.Content.Res;
using System.IO;
using SQLite;
using DABApp.Droid;
using Xamarin.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

[assembly: Dependency(typeof(SQLite_Droid))]
namespace DABApp.Droid
{
    public class SQLite_Droid : ISQLite
    {
        public static AssetManager Assets { get; set; }

        private bool _initiated = false;

        public SQLiteConnection GetConnection(bool ResetDatabaseOnStart)
        {
         

            var connection = new SQLiteConnection(GetDatabasePath(ResetDatabaseOnStart));
            return connection;
        }

        public SQLiteAsyncConnection GetAsyncConnection(bool ResetDatabaseOnStart)
        {


            var connection = new SQLiteAsyncConnection(GetDatabasePath(ResetDatabaseOnStart));
            return connection;
        }

        private string GetDatabasePath(bool ResetDatabaseOnStart)
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
                DirectoryInfo dir = new DirectoryInfo(folder);
                foreach (FileInfo fil in dir.GetFiles(("*.db3")))
                {
                    if (fil.Name != filename)
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
                                        cn.Close();
                                        if (GlobalResources.SettingsToPreserve == null)
                                        {
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
                                                        case "Token":
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
