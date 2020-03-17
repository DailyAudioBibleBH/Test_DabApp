using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DABApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DabAchievementsPage : DabBaseContentPage
	{
		public DabAchievementsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
		}
	}
}