using System;
using Xamarin.Forms;

namespace DABApp
{
	public static class ControlTemplateAccess
	{
		public static T FindTemplateElementByName<T>(this Page page, string Name)
		where T : Element
		{
			var pc = page as IPageController;
			if (pc == null)
			{
				return null;
			}

			foreach (var child in pc.InternalChildren)
			{
				var result = child.FindByName<T>(Name);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}
	}
}
