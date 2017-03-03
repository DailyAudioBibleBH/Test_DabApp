using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEpisodesPage : DabBaseContentPage
	{
		public DabEpisodesPage(Resource resource)
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
		}

		//void OnMenu(object o, EventArgs e) {
		//	this.ShowMenu();
		//}
	}
}
