using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
    public static class ViewExtensions
    {

        /* Animation to change the color of an object
         * Sample Usage:
         * stackTodaysReading.ColorTo(Color.Blue, Color.Red, c => stackTodaysReading.BackgroundColor = c, 1000, Easing.Linear);
         * */

        public static Task<bool> ColorTo(this VisualElement self, Color fromColor, Color toColor, Action<Color> callback, uint length = 250, Easing easing = null)
        {
            Func<double, Color> transform = (t) =>
              Color.FromRgba(fromColor.R + t * (toColor.R - fromColor.R),
                             fromColor.G + t * (toColor.G - fromColor.G),
                             fromColor.B + t * (toColor.B - fromColor.B),
                             fromColor.A + t * (toColor.A - fromColor.A));
            return ColorAnimation(self, "ColorTo", transform, callback, length, easing);
        }

        public static void CancelColorToAnimation(this VisualElement self)
        {
            self.AbortAnimation("ColorTo");
        }

        static Task<bool> ColorAnimation(VisualElement element, string name, Func<double, Color> transform, Action<Color> callback, uint length, Easing easing)
        {
            easing = easing ?? Easing.Linear;
            var taskCompletionSource = new TaskCompletionSource<bool>();

            element.Animate<Color>(name, transform, callback, 16, length, easing, (v, c) => taskCompletionSource.SetResult(c));
            return taskCompletionSource.Task;
        }

        /* Animation to change the height of an object (not scale or translate)
         * Sample Usage:
         * stackTodaysReading.HeightTo(0, TodaysReadingHeight, h => stackTodaysReading.HeightRequest = h, 1000, Easing.Linear);
         * */

        public static Task<bool> HeightTo(this VisualElement self, double fromHeight, double toHeight, Action<double> callback, uint length = 250, Easing easing = null)
        {
            Func<double, double> transform = (t) => t * (toHeight - fromHeight);

            return HeightAnimation(self, "HeightTo", transform, callback, length, easing);
        }

        public static void CancelHeightToAnimation(this VisualElement self)
        {
            self.AbortAnimation("HeightTo");
        }

        static Task<bool> HeightAnimation(VisualElement element, string name, Func<double, double> transform, Action<double> callback, uint length, Easing easing)
        {
            easing = easing ?? Easing.Linear;
            var taskCompletionSource = new TaskCompletionSource<bool>();

            element.Animate<double>(name, transform, callback, 16, length, easing, (v, c) => taskCompletionSource.SetResult(c));
            return taskCompletionSource.Task;
        }
    }
}
