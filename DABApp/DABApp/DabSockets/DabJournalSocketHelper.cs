using System;
namespace DABApp.DabSockets
{
    //This class helps build JSON objects for passing back and forth to the journal socket

    public class DabJournalObject
    {
        public string html { get; set; }
        public string date { get; set; }
        public string token { get; set; }
        public string content { get; set; }

        public DabJournalObject(string Date, string Token)
        {
            date = Date;
            token = Token;
        }

        public DabJournalObject(string Html, string Date, string Token)
        {
            html = Html;
            date = Date;
            token = Token;
        }
    }
}


