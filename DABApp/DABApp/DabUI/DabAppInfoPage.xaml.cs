﻿using System;
using System.Collections.Generic;
using Version.Plugin;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAppInfoPage : DabBaseContentPage
	{
		public DabAppInfoPage()
		{
			InitializeComponent();
			BindingContext = ContentConfig.Instance.blocktext;
			VersionNumber.Text = $"Version Number {CrossVersion.Current.Version}";
		}
	}
}
