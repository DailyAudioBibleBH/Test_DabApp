﻿using System;
using System.Collections.Generic;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbCampaigns
    {
        [PrimaryKey]
        public int campaignId { get; set; }
        public int campaignWpId { get; set; }
        public string campaignTitle { get; set; }
        public string campaignDescription { get; set; }
        public string campaignStatus { get; set; }
        public double campaignSuggestedSingleDonation { get; set; }
        public double campaignSuggestedRecurringDonation { get; set; }
        public string pricingPlans { get; set; }
        public bool @default {get;set;}
        //TODO: what are pricing plans

        public dbCampaigns(DabGraphQlCampaign camp)
        {
            this.campaignId = Convert.ToInt32(camp.id);
            this.campaignWpId = camp.wpId;
            this.campaignTitle = camp.title;
            this.campaignDescription = camp.description;
            this.campaignStatus = camp.status;
            this.campaignSuggestedSingleDonation = camp.suggestedSingleDonation;
            this.campaignSuggestedRecurringDonation = camp.suggestedRecurringDonation;
            this.@default = camp.@default;
            if (camp.pricingPlans != null)
            {
                this.pricingPlans = camp.pricingPlans.ToString();
            }
        }

        public dbCampaigns()
        {

        }

        public dbCampaigns(DabGraphQlUpdateCampaign camp)
        {
            this.campaignId = Convert.ToInt32(camp.id);
            this.campaignWpId = camp.wpId;
            this.campaignTitle = camp.title;
            this.campaignDescription = camp.description;
            this.campaignStatus = camp.status;
            this.campaignSuggestedSingleDonation = camp.suggestedSingleDonation;
            this.campaignSuggestedRecurringDonation = camp.suggestedRecurringDonation;
            this.@default = camp.@default;
            if (camp.pricingPlans != null)
            {
                this.pricingPlans = camp.pricingPlans.ToString();
            }
        }
    }
}
