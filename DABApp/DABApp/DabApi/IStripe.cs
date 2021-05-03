using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
	public interface IStripe
	{
		Task<StripeContainer> AddCard(Card newCard);
	}
}
