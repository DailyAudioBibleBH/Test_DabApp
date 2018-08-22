using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(SwitchCell), typeof(DabSwitchCellRenderer))]
namespace DABApp.iOS
{
	public class DabSwitchCellRenderer: SwitchCellRenderer
	{
		public override UIKit.UITableViewCell GetCell(Cell item, UIKit.UITableViewCell reusableCell, UIKit.UITableView tv)
		{
			var s = item as SwitchCell;
			var cell = base.GetCell(item, reusableCell, tv);
			UISwitch uiSwitch = cell.AccessoryView as UISwitch;
			uiSwitch.OnTintColor = ((Color)App.Current.Resources["HighlightColor"]).ToUIColor();
			cell.BackgroundColor = ((Color)App.Current.Resources["InputBackgroundColor"]).ToUIColor();
            UIFont font = UIFont.FromName("Helvetica", 18.5f);
            cell.TextLabel.Font = font;
			cell.TextLabel.TextColor = ((Color)App.Current.Resources["TextColor"]).ToUIColor();
			return cell;
		}
	}
}
