using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicDetails : DabBaseContentPage
	{
		public DabForumPhoneTopicDetails(Topic topic)
		{
			InitializeComponent();
			DetailsView.BindingContext = topic;
			DetailsView.replies.ItemsSource = topic.replies;
			var dateTime = Convert.ToDateTime(topic.replies.OrderBy(x => x.gmtDate).First().gmtDate);
			var month = dateTime.ToString("MMMM");
			var time = dateTime.TimeOfDay.ToString();
			DetailsView.last.Text = $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
		}
	}
}
