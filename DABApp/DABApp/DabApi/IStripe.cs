using System;
using System.Threading.Tasks;

namespace DABApp
{
	public interface IStripe
	{
		Task<string> AddCard(Card newCard);
		Task DeletCard(Card card);
	}
}
