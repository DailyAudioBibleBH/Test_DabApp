using System;
using SQLite;

namespace DABApp
{
    public class dbUserCampaigns
    {
        [PrimaryKey]
        public int donationId { get; set; }
        public int donationUserId { get; set; }
        public int donationWpId { get; set; }
        public string donationSource { get; set; }
        public double donationAmount { get; set; }
        public string donationRecurringInterval { get; set; }
        public int donationCampaignWpId { get; set; }
        public int UserWpId { get; set; }
        public string donationStatus { get; set; }

        public dbUserCampaigns()
        {
        }
    }
}
