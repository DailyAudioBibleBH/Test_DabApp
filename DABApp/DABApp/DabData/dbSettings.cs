using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SQLite;

namespace DABApp
{
    public class dbSettings
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }

        public static async Task<bool> DeleteLoginSettings()
        {
            try
            {
                SQLite.SQLiteAsyncConnection adb = DabData.AsyncDatabase;
                var a = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;
                a.Token = "";
                a.TokenCreation = DateTime.MinValue;
                a.FirstName = "";
                a.LastName = "";
                a.Email = "";
                a.ActionDate = GlobalResources.DabMinDate;
                a.ProgressDate = GlobalResources.DabMinDate;
                await adb.InsertOrReplaceAsync(a);
            }
            catch (Exception ex)
            {
                //ignore exceptions
            }
            return true;
        }

        public static string GetSetting(string Key, string DefaultValue)
        {
            //Gets a setting from the database or uses the default value if not found.
            try
            {
                SQLite.SQLiteAsyncConnection adb = DabData.AsyncDatabase;
                var s = adb.Table<dbSettings>().Where(x => x.Key.ToLower() == Key.ToLower()).FirstOrDefaultAsync().Result;
                if (s != null)
                {
                    return s.Value;
                }
                else
                {
                    return DefaultValue;
                }
            }
            catch (Exception ex)
            {
                return DefaultValue;
            }

        }

        public async static void StoreSetting(string Key, string Value)
        {
            //Stores a setting in the database by updating or adding it.
            try
            {
                SQLite.SQLiteAsyncConnection adb = DabData.AsyncDatabase;

                //We store null values as empty strings.
                if (Value == null)
                {
                    Value = "";
                    return;
                }

                //Find the existing setting
                var s = adb.Table<dbSettings>().Where(x => x.Key == Key).FirstOrDefaultAsync().Result;
                if (s != null) //found it!
                {
                    //update
                    s.Value = Value;
                    await adb.UpdateAsync(s);
                    return;
                }
                else //didn't find it!
                {
                    //insert
                    s = new dbSettings() { Key = Key, Value = Value };
                    await adb.InsertOrReplaceAsync(s);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception while saving setting [{Key}] with value '{Value}': {ex.ToString()}");
                return;
            }
        }

    }


}
