//using System;
//namespace DABApp.Helpers
//{
//    public static class MonthNameHelper
//    {
//        public static string MonthNameFromNumber(int Number)
//        {
//            /* returns the month name from a given month number */
//            try
//            {
//                return new DateTime(DateTime.Now.Year, Number, 1).ToString("MMMM");
//            }
//            catch (Exception ex)
//            {
//                return "Unknown"; //unknown month 
//            }

//        }

//        public static int MonthNumberFromName(string Name)
//        {
//            /* returns the month name from a given month number */
//            try
//            {
//                return DateTime.Parse($"{Name} 1, {DateTime.Now.Year}").Month;
//            }
//            catch (Exception ex)
//            {
//                return 0; //unknown month name
//            }

//        }

//    }
//}
