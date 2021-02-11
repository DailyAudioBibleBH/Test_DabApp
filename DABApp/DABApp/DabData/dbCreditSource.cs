using System;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbCreditSource
    {
        [PrimaryKey]
        public string donationId { get; set; }
        public string cardId { get; set; }
        public string processor { get; set; }
        public string next { get; set; }

        public dbCreditSource()
        {
        }

        public dbCreditSource(DabGraphQlDonationSource source, string DonationId)
        {
            this.cardId = source.cardId;
            this.donationId = DonationId;
            this.processor = source.processor;
            this.next = source.next;
        }
    }
}
