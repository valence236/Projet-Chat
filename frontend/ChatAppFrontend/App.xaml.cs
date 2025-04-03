using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace ChatAppFrontend
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if DEBUG
            AllocConsole(); // ✅ Ouvre la console uniquement en mode debug
#endif
        }
    }
}
