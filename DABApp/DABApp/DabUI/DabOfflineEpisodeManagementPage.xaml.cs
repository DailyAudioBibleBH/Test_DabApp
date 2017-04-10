using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabOfflineEpisodeManagementPage : DabBaseContentPage
	{
		public DabOfflineEpisodeManagementPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
		}
	}
}
