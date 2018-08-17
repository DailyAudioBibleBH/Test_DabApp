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
		SwitchCellView _view;
		Android.Widget.Switch Switch;

		protected override Android.Views.View GetCellCore(Cell item, Android.Views.View convertView, Android.Views.ViewGroup parent, Android.Content.Context context)
		{
			var cell = base.GetCellCore(item, convertView, parent, context);

			if ((_view = convertView as SwitchCellView) == null)
				_view = new SwitchCellView(context, item);

			_view.Cell = (SwitchCell)Cell;

			cell.SetBackgroundColor(((Color)App.Current.Resources["InputBackgroundColor"]).ToAndroid());
			var child1 = ((LinearLayout)cell).GetChildAt(1);
			var child2 = ((LinearLayout)cell).GetChildAt(2);

			var label = (TextView)((LinearLayout)child1).GetChildAt(0);
			label.SetTextColor(((Color)App.Current.Resources["TextColor"]).ToAndroid());

			var swit = (Android.Widget.Switch)child2;
			OnChecked(swit);
			swit.CheckedChange += (sender, e) => { OnChecked(swit);};
			Switch = swit;
			_view.Cell.OnChanged += OnElementChanged;

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
			_view.Cell.On = s.Checked;
		}

		void OnElementChanged(object sender, EventArgs e)
		{
			Switch.Checked = _view.Cell.On;
		}
	}
}
