using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CoreGraphics;
using DABApp;
using DABApp.iOS.CustomRenderers;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(DabViewCell), typeof(DabMenuItemRenderer))]
namespace DABApp.iOS.CustomRenderers
{
    public class DabMenuItemRenderer: ViewCellRenderer
    {
        public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
        {
            var cell = base.GetCell(item, reusableCell, tv);

            try
            {
                // This is the assembly full name which may vary by the Xamarin.Forms version installed.
                // NullReferenceException is raised if the full name is not correct.
                var globalContextViewCell = Type.GetType("Xamarin.Forms.Platform.iOS.ContextActionsCell, Xamarin.Forms.Platform.iOS, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null");

                // Now change the static field value! "NormalBackground" OR "DestructiveBackground"
                if (globalContextViewCell != null)
                {
                    //var normalButton = globalContextViewCell.GetField("NormalBackground", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    //if (normalButton != null)
                    //{
                    //    normalButton.SetValue(null, getImageBasedOnColor("4fb8bd"));
                    //}

                    var destructiveButton = globalContextViewCell.GetField("DestructiveBackground", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (destructiveButton != null)
                    {
                        destructiveButton.SetValue(null, getImageBasedOnColor("4fb8bd"));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error in setting background color of Menu Item : " + e.ToString());
            }

            return cell;
        }

        private UIImage getImageBasedOnColor(string colorCode)
        {
            // Get UIImage with a green color fill
            CGRect rect = new CGRect(0, 0, 1, 1);
            CGSize size = rect.Size;
            UIGraphics.BeginImageContext(size);
            CGContext currentContext = UIGraphics.GetCurrentContext();
            currentContext.SetFillColor(Color.FromHex(colorCode).ToCGColor());
            currentContext.FillRect(rect);
            var backgroundImage = UIGraphics.GetImageFromCurrentImageContext();
            currentContext.Dispose();

            return backgroundImage;
        }
    }
}