using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabBrowserPage : DabBaseContentPage
	{
		string url = "http://c2itconsulting.net/";

		public DabBrowserPage()
		{
			InitializeComponent();
			Browser.Source = url;
		}

		void OnBack(object o, EventArgs e) {
			if (Browser.CanGoBack)
			{
				Browser.GoBack();
			}
		}

		void OnForward(object o, EventArgs e) {
			if (Browser.CanGoForward) {
				Browser.GoForward();
			}
		}

		void OnBrowser(object o, EventArgs e) {
			Device.OpenUri(new Uri(url));
		}
	}
}
