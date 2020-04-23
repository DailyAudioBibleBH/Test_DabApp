using System;
using System.Threading.Tasks;
using SQLite;

namespace DABApp
{
	public class dbSettings
	{
		[PrimaryKey]
		public string Key { get; set;}
		public string Value { get; set;}

        public static async Task<bool> DeleteLoginSettings()
        {
            try
            {
                await DeleteSetting("Token");
                await DeleteSetting("TokenCreation");
                await DeleteSetting("FirstName");
                await DeleteSetting("LastName");
                await DeleteSetting("Email");
            }
            catch (Exception ex)
            {
                //ignore exceptions
            }
            return true;
        }

        public static async Task<bool> DeleteSetting(string Key)
        {
            try
            {
                SQLite.SQLiteAsyncConnection adb = DabData.AsyncDatabase;
                var s = adb.Table<dbSettings>().Where(x => x.Key == Key).FirstOrDefaultAsync().Result;
                if (s != null)
                {
                    await adb.DeleteAsync(s);
                }
            }
            catch (Exception ex)
            {
                //ignore exceptions
            }
            return true;

        }
    }


}
