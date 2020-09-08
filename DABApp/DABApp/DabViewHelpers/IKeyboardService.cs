using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabViewHelpers
{
    public interface IKeyboardService
    {
        event EventHandler KeyboardIsShown;
        event EventHandler KeyboardIsHidden;
    }
}
