using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerView : MenuContainerPage
	{
		public DabPlayerView()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
		}
	}
}
