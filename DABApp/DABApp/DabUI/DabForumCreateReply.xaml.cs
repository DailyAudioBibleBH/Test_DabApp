using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumCreateReply : DabBaseContentPage
	{
		public DabForumCreateReply(Topic topic)
		{
			InitializeComponent();
			BindingContext = topic;
		}

		async void OnPost(object o, EventArgs e)
		{ 
			
		}
	}
}
