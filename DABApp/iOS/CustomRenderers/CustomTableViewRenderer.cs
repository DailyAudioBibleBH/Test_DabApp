using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(TableView), typeof(CustomTableViewRenderer))]
namespace DABApp.iOS
{
	public class CustomTableViewRenderer: TableViewRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<TableView> e)
		{
			base.OnElementChanged(e);
			if (Control == null)
				return;
			var tableView = Control as UITableView;
			tableView.SeparatorColor = ((Color)App.Current.Resources["EpisodeMenuColor"]).ToUIColor();
		}
	}
}
