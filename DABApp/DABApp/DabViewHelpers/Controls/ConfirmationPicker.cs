using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace DABApp
{
    public class ConfirmationPicker : Picker
    {
        public event EventHandler Submitted;

        public void Submission(object o, EventArgs e)
        {
            Submitted.Invoke(o, e);
        }
    }
}
