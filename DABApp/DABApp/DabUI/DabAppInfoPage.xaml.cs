using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAppInfoPage : DabBaseContentPage
	{
		public DabAppInfoPage()
		{
			InitializeComponent();
			BindingContext = ContentConfig.Instance.blocktext;
		}
	}
}
