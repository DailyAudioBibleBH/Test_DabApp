﻿using System;
using System.Threading.Tasks;
using DABApp.Droid;
using Stripe;
using Xamarin.Forms;

[assembly: Dependency(typeof(StripeApiManagement))]
namespace DABApp.Droid
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