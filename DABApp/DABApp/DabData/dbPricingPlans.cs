using System;
using DABApp.DabSockets;
using SQLite;

namespace DABApp
{
    public class dbPricingPlans
    {
        public dbPricingPlans(DabGraphQlPricingPlan plan)
        {
            this.id = plan.id;
            this.type = plan.type;
            this.amount = plan.amount;
            this.recurring = plan.recurring;
        }

        public dbPricingPlans()
        {

        }

        [PrimaryKey]
        public string id { get; set; }
        public string type { get; set; }
        public double amount { get; set; }
        public bool recurring { get; set; }
    }
}
