using System;
using SQLite;

namespace DABApp
{
    public class dbDataTransfers
    {
        /*
         * this class logs data transfers to the database
         */

        public dbDataTransfers()
        {
        }

        [AutoIncrement, PrimaryKey]
        public long Id { get; set; }
        [Indexed]
        public DateTime LogTimestamp { get; set; }
        public String Direction { get; set; }
        public string Data { get; set; }

        public static async void LogTransfer(string direction, string data)
        {
            try
            {
                SQLite.SQLiteAsyncConnection adb = DabData.AsyncDatabase;
                await adb.InsertAsync(new dbDataTransfers()
                {
                    Data = data,
                    Direction = direction,
                    LogTimestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                //ignore exceptions
            }

        }
    }
}
