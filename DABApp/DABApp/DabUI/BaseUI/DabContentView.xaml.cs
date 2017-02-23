using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabContentView : DabBaseContentPage
	{
		public DabContentView(DABApp.View contentView)
		{
			InitializeComponent();
			this.BindingContext = contentView;
			if (Device.Idiom == TargetIdiom.Phone)
			{
				banner.Source = contentView.banner.urlPhone;
			}
			else{
				banner.Source = contentView.banner.urlTablet;
			}
		}
	}
}
