using System;
using System.IO;

namespace TLauncher {
    partial class Program {
        static void Main(string[] args) {
            Console.Title = "TLauncher - By Marcussacana";
            if (File.Exists(Common.Setup)) {
#if !DEBUG
                IntPtr Handle = Unmanaged.GetConsoleWindow();
                Unmanaged.ShowWindow(Handle, Unmanaged.SW_HIDE);
#endif
                Replacer.InitializeHook(args);
                return;
            }
            Console.WriteLine("Welcome to the TLauncher");
            Console.WriteLine("Launcher Data not found... Starting Creation Mode...");
            Dumper.SetupLauncher(args);
        }
        
    }
}
