using System;
using SQLite;

namespace DABApp
{
    public class dbCreditCards
    {
        [PrimaryKey]
        public int cardWpId { get; set; }
        public int cardUserId { get; set; }
        public int cardLastFour { get; set; }
        public int cardExpMonth { get; set; }
        public int cardExpYear { get; set; }
        public string? cardType { get; set; }
        public string cardStatus { get; set; }

        public dbCreditCards()
        {
        }
    }
}
