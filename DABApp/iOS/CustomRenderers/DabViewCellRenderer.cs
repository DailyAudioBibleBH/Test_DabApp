using System;
using DABApp;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(ViewCell), typeof(DabViewCellRenderer))]
namespace DABApp.iOS
{
	public class DabViewCellRenderer: ViewCellRenderer
	{
		public override UIKit.UITableViewCell GetCell(Cell item, UIKit.UITableViewCell reusableCell, UIKit.UITableView tv)
		{
			var cell = base.GetCell(item, reusableCell, tv);
            cell.SeparatorInset = UIEdgeInsets.Zero;

			cell.SelectedBackgroundView = new UIView
			{
				BackgroundColor = UIColor.Black.ColorWithAlpha(.1F)
			};

			return cell;
		}
	}
}
