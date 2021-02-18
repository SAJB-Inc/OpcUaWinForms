using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaWinForms.Win32 {
    public static class WindowApi {
        [DllImport("user32.dll")]
        public static extern int FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        public static extern int ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        public static extern int FindWindowEx(int parentHandle, int childAfter, string className, int windowTitle);

        //[DllImport("user32.dll")]
        //public static extern int GetDesktopWindow();

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 1;
    }
}
