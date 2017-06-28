using System;
using System.Threading.Tasks;

namespace DABApp
{
	public interface IStripe
	{
		Task<StripeContainer> AddCard(DABApp.Card newCard);
	}
}
