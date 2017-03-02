using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTermsAndConditionsPage : DabBaseContentPage
	{
		public DabTermsAndConditionsPage()
		{
			InitializeComponent();
			BindingContext = ContentConfig.Instance.blocktext;

			this.ToolbarItems.Clear();
		}
	}
}
