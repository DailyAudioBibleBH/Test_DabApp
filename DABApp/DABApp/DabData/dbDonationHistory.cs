using System;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbDonationHistory
    {
        [PrimaryKey]
        public string historyId { get; set; }

        public int historyWpId { get; set; }

        public string historyPlatform { get; set; }

        public string historyPaymentType { get; set; }

        public string historyChargeId { get; set; }

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

        public dbDonationHistory(DabGraphQlSingleDonationHistory d)
        {
            this.historyId = d.id;
            this.historyWpId = d.wpId;
            this.historyPlatform = d.platform;
            this.historyPaymentType = d.paymentType;
            this.historyChargeId = d.chargeId;
            this.historyDate = d.date;
            this.historyDonationType = d.donationType;
            this.historyCurrency = d.currency;
            this.historyGrossDonation = d.grossDonation;
            this.historyFee = d.fee;
            this.historyNetDonation = d.netDonation;
            this.historyCampaignWpId = d.campaignWpId;
            this.historyUserWpId = d.userWpId;
        }
    }
}
