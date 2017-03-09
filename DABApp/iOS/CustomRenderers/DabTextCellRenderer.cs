using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(TextCell), typeof(DabTextCellRenderer))]
namespace DABApp.iOS
{
	public class DabTextCellRenderer: TextCellRenderer
	{
		public DabTextCellRenderer()
		{
		}

		protected override void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
		{
			base.HandlePropertyChanged(sender, args);
		}

		public override UIKit.UITableViewCell GetCell(Cell item, UIKit.UITableViewCell reusableCell, UIKit.UITableView tv)
		{
			UITableViewCell cell = base.GetCell(item, reusableCell, tv);
			UIFont font = cell.TextLabel.Font;

			cell.TextLabel.TextColor = ((Color)App.Current.Resources["TextColor"]).ToUIColor();
			cell.TextLabel.Font = font.WithSize(25.0f);
			cell.DetailTextLabel.TextColor = ((Color)App.Current.Resources["SecondaryTextColor"]).ToUIColor();
			cell.SelectedBackgroundView = new UIView
			{
				//lightly shaded background when selected
				BackgroundColor = UIColor.Black.ColorWithAlpha(.1F)
			};

			//// Found with this link http://stackoverflow.com/questions/25885238/xamarin-forms-listview-set-the-highlight-color-of-a-tapped-item/26896715#26896715
			//cell.SelectedBackgroundView = new UIView
			//{
			//	BackgroundColor = UIColor.FromRGB(50, 150, 107),
			//};

			return cell;

			//return base.GetCell(item, reusableCell, tv);

		}

		
	}
}
