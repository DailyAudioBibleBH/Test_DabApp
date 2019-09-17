using System;
namespace DABApp.DabSockets
{
    public class DabJournalSocketHelper
    {
        public string html { get; set; }
        public string date { get; set; }
        public string token { get; set; }

        public DabJournalSocketHelper(string Date, string Token)
        {
            date = Date;
            token = Token;
        }

        public DabJournalSocketHelper(string Html, string Date, string Token)
        {
            html = Html;
            date = Date;
            token = Token;
        }
    }
}


