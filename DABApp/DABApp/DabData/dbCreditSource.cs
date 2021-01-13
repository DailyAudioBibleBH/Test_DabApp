using System;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbCreditSource
    {
        [PrimaryKey]
        public string cardId { get; set; }
        public string processor { get; set; }
        public string next { get; set; }

        public dbCreditSource()
        {
        }

        public dbCreditSource(DabGraphQlDonationSource source)
        {
            this.cardId = source.cardId;
            this.processor = source.processor;
            this.next = source.next;
        }
    }
}
