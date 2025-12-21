using System;

namespace MacroApp.Models
{
    public class MacroStatusEventArgs : EventArgs
    {
        public string Code { get; }
        public string Detail { get; }

        public MacroStatusEventArgs(string code, string detail = null)
        {
            Code = code;
            Detail = detail;
        }
    }
}