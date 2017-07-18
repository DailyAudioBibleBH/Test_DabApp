using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumCreateTopic : DabBaseContentPage
	{
		public DabForumCreateTopic()
		{
			InitializeComponent();
			Content.HeightRequest = this.Height;
		}

		async void OnPost(object o, EventArgs e)
		{ 
			
		}
	}
}
