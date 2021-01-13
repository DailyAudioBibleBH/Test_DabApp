using System;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbUserCampaigns
    {
        [PrimaryKey]
        public string Id { get; set; }
        //public int donationUserId { get; set; }
        public int WpId { get; set; }
        public string Source { get; set; }
        public double Amount { get; set; }
        public string RecurringInterval { get; set; }
        public int CampaignWpId { get; set; }
        public int UserWpId { get; set; }
        public string Status { get; set; }

        public dbUserCampaigns()
        {
        }

        public dbUserCampaigns(DabGraphQlDonation d)
        {
            this.Id = d.id;
            //TODO: ask chet but I think tdd had an extra property here
            //this.donationUserId = d.userWpId;
            this.WpId = d.wpId;
            this.Source = d.source.cardId;//cardid TODO:come back to this
            this.Amount = d.amount;
            this.RecurringInterval = d.recurringInterval;
            this.CampaignWpId = d.campaignWpId;
            this.UserWpId = d.userWpId;
            this.Status = d.status;
        }
    }
}
