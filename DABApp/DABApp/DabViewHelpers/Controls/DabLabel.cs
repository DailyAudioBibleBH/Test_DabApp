using System;
using System.Diagnostics;
using Xamarin.Forms;

namespace DABApp
{
	public class DabLabel: Label
	{
		public static readonly BindableProperty IsTitleProperty = BindableProperty.Create("IsTitle", typeof(bool), typeof(bool), false);
		string Title;
		string Desc;

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			if (Device.RuntimePlatform == "Android")
			{
				if ((bool)GetValue(IsTitleProperty))
				{
					if (BindingContext == null)
					{
						Text = Title;
					}
					else {
						Title = ((EpisodeViewModel)BindingContext).title;
					}
				}
				else
				{
					if (BindingContext == null)
					{
						Text = Desc;
					}
					else 
					{
						Desc = ((EpisodeViewModel)BindingContext).description;
					}
				}
			}
		}
	}
}
