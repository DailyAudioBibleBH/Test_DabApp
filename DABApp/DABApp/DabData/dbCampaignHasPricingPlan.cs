using System;
namespace DABApp
{
    public class dbCampaignHasPricingPlan
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public int PricingPlanId { get; set; }

        public dbCampaignHasPricingPlan()
        {
        }
    }
}
