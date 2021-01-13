using System;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbUserCampaigns
    {
        [PrimaryKey]
        public string donationId { get; set; }
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

        public dbUserCampaigns(DabGraphQlDonation d)
        {
            this.donationId = d.id;
            this.donationUserId = d.userWpId;
            this.donationWpId = d.wpId;
            this.donationSource = d.source.cardId;//cardid TODO:come back to this
            this.donationAmount = d.amount;
            this.donationRecurringInterval = d.recurringInterval;
            this.donationCampaignWpId = d.campaignWpId;
            this.UserWpId = d.userWpId;
            this.donationStatus = d.status;
        }
    }
}
