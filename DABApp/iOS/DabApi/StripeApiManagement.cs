using System;
using DABApp.iOS;
using Xamarin.Forms;
using Stripe;
using System.Threading.Tasks;

[assembly: Dependency(typeof(StripeApiManagement))]
namespace DABApp.iOS
{
	public class StripeApiManagement : IStripe
	{
		public async Task<string> AddCard(Card newCard)
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
				var token = await StripeClient.CreateToken(sCard);
				return token.Id;
			}
			catch (Exception ex)
			{
				return $"Error: {ex.Message}";
			}
		}

		public async Task DeleteCard(Card card)
		{

		}
	}
}
