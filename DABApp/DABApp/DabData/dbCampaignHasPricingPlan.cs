using System;
using SQLite;

namespace DABApp
{
    public class dbCampaignHasPricingPlan
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public int CampaignWpId { get; set; }
        public string PricingPlanId { get; set; }

        public dbCampaignHasPricingPlan()
        {
        }
    }
}
