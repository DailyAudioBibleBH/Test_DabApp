using System;
using System.Collections.Generic;

namespace DABApp
{
	public class MenuItem { 
		public string Title { get; set;}
		public Type TargetType { get; set;}
	}

	public class MenuListData : List<MenuItem>
	{
		public MenuListData()
		{
			this.Add(new MenuItem()
			{
				Title = "Channels",
				TargetType = typeof(DabChannelsPage)
			});

			this.Add(new MenuItem()
			{
				Title = "About",
				TargetType = typeof(DabAboutPage)
			});

			//this.Add(new MenuItem()
			//{
			//	Title = "Accounts",
			//	IconSource = "accounts.png",
			//	TargetType = typeof(AccountsPage)
			//});

			//this.Add(new MenuItem()
			//{
			//	Title = "Opportunities",
			//	IconSource = "opportunities.png",
			//	TargetType = typeof(OpportunitiesPage)
			//});
		}
	}
}
