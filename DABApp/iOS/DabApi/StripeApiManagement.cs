using System;
using DABApp.iOS;
using Xamarin.Forms;
using Stripe;
using System.Threading.Tasks;
using Newtonsoft.Json;

[assembly: Dependency(typeof(StripeApiManagement))]
namespace DABApp.iOS
{
	public class StripeApiManagement : IStripe
	{
		public async Task<StripeContainer> AddCard(Card newCard)
		{
			var sCard = new Stripe.Card
			{
				Number = newCard.fullNumber,
				ExpiryMonth = newCard.exp_month,
				ExpiryYear = newCard.exp_year,
				CVC = newCard.cvc
			};
			try
			{
                if (!GlobalResources.TestMode)
                {
					Stripe.StripeClient.DefaultPublishableKey = GlobalResources.StripeApiKey;
				}
				else
                {
					Stripe.StripeClient.DefaultPublishableKey = GlobalResources.StripeTestApiKey;
				}
				var token = await StripeClient.CreateToken(sCard);
				var container = new StripeContainer();
				container.card_token = token.Id;
				return container;
			}
			catch (Exception ex)
			{
				var container = new StripeContainer();
				container.card_token = $"Error: {ex.Message}";
				return container;
			}
		}
	}
}
