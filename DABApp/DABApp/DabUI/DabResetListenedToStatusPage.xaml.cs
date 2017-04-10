using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabResetListenedToStatusPage : DabBaseContentPage
	{
		public DabResetListenedToStatusPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
		}
	}
}
