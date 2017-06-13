using System;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public class MasterDetailMenuPage : MasterDetailPage, IMenuContainerPage
	{
		public MasterDetailMenuPage() {
			this.MasterBehavior = MasterBehavior.Split;
		}

		public Action HideMenuAction { get; set; }

		public Action ShowMenuAction { get; set; }

		SlideMenuView slideMenu;
		public SlideMenuView SlideMenu
		{
			get
			{
				return slideMenu;
			}

			set
			{
				if (slideMenu != null)
					slideMenu.Parent = null;
				slideMenu = value;
				if (slideMenu != null)
					slideMenu.Parent = this;
			}
		}

		public void ShowMenu()
		{
			if (ShowMenuAction != null)
				ShowMenuAction();
		}

		public void HideMenu()
		{
			if (HideMenuAction != null)
				HideMenuAction();
		}
	}
}
