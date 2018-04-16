using System;
using Xamarin.Forms;

namespace DABApp
{
	public class HtmlLabel: Label
	{
		public bool EraseText { get; set; } = false;
        public bool IsSelectable { get; set; } = true;
	}
}
