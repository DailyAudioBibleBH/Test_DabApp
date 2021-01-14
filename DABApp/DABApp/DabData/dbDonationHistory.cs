using System;
using SQLite;

namespace DABApp
{
    public class dbDonationHistory
    {
        [PrimaryKey]
        public int historyId { get; set; }

        public int historyWpId { get; set; }

        public string historyPlatform { get; set; }

        public string historyPaymentType { get; set; }

        public int historyChargeId { get; set; }

        public DateTime historyDate { get; set; }

        public string historyDonationType { get; set; }

        public string historyCurrency { get; set; }

        public double historyGrossDonation { get; set; }

        public double historyFee { get; set; }

        public double historyNetDonation { get; set; }

        public int historyCampaignWpId { get; set; }

        public int historyUserWpId { get; set; }

        public dbDonationHistory()
        {
        }
    }
}
