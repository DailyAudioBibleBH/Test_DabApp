using System;
using DABApp.DabSockets;
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

        public dbCreditCards(DabGraphQlCreditCard card)
        {
            this.cardWpId = card.wpId;
            this.cardUserId = card.userId;
            this.cardLastFour = card.lastFour;
            this.cardExpMonth = card.expMonth;
            this.cardExpYear = card.expYear;
            this.cardType = card.type;
            this.cardStatus = card.status;
        }
    }
}
