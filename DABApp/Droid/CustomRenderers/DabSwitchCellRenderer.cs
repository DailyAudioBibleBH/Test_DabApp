using System;
using Android.Widget;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(SwitchCell), typeof(DabSwitchCellRenderer))]
namespace DABApp.Droid
{
	public class DabSwitchCellRenderer: SwitchCellRenderer
	{
		Android.Widget.Switch _view;

		protected override Android.Views.View GetCellCore(Cell item, Android.Views.View convertView, Android.Views.ViewGroup parent, Android.Content.Context context)
		{
			var cell = base.GetCellCore(item, convertView, parent, context);

			cell.SetBackgroundColor(((Color)App.Current.Resources["InputBackgroundColor"]).ToAndroid());
			var child1 = ((LinearLayout)cell).GetChildAt(1);
			var child2 = ((LinearLayout)cell).GetChildAt(2);

			var label = (TextView)((LinearLayout)child1).GetChildAt(0);
			label.SetTextColor(((Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid());

			var swit = (Android.Widget.Switch)child2;
			OnChecked(swit);
			swit.CheckedChange += (sender, e) => { OnChecked(swit);};
			_view = swit;

			return cell;
		}

		void OnChecked(Android.Widget.Switch s)
		{
			if (s.Checked)
			{
				s.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["HighlightColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
			else
			{
				s.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["TextColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
		}

		protected override void OnCellPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
		{
			base.OnCellPropertyChanged(sender, args);
			if (args.PropertyName == SwitchCell.OnProperty.PropertyName)
			{
				_view.Checked = ((SwitchCell)Cell).On;
			}
		}
	}
}
