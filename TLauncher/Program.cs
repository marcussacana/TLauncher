using AdvancedBinary;
using System;
using System.Diagnostics;
using System.IO;
using static TLauncher.Common;

namespace TLauncher {
    partial class Program {
        static void Main(string[] args) {
            Console.Title = "TLauncher - By Marcussacana";
            WorkArgs(args);

            if (File.Exists(Setup)) {
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

        private static void WorkArgs(string[] Args) {
            if (Args?.Length == 0)
                return;

            foreach (string arg in Args) {
                string Argument = arg.Trim(' ', '-', '/', '\\');
                switch (Argument) {
                    case "hook":
                    case "rehook":
                    case "updatetl":
                    case "updatetranslation":
                    case "edittl":
                    case "edittranslation":
                    case "resumetranslation":
                        ContinueTL();
                        break;

                    case "auto":
                    case "google":
                    case "mtl":
                        AutoTL();
                        break;

                    case "help":
                    case "?":
                        Console.WriteLine("Use the argument -Hook to append text to the translation");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void AutoTL() {
            Replacer.MTL = true;
            Console.WriteLine("Translate From:");
            Replacer.From = Console.ReadLine();
            Console.WriteLine("Translate To:");
            Replacer.To = Console.ReadLine();
            Console.WriteLine("Executable Path:");
            string Exe = Console.ReadLine();
            
            Replacer.InitializeHook(new string[] { Exe });
            Environment.Exit(0);
        }

        private static void ContinueTL() {
            Console.WriteLine("Loading Translation...");
            StructReader Reader = new StructReader(Setup);
            Config CurrentSetup = new Config();
            Reader.ReadStruct(ref CurrentSetup);
            Reader.Close();

            for (uint i = 0; i < CurrentSetup.Strings.LongLength; i++) {
                Dumper.Texts.Add(CurrentSetup.Strings[i]);
                Dumper.Tls.Add(CurrentSetup.TLs[i]);
            }

            Dumper.Delay = CurrentSetup.Delay;
            Dumper.Exe = CurrentSetup.Executable;
            Dumper.Invalidate = CurrentSetup.Invalidate > 0;


            File.Delete(Setup);

            Dumper.SetupLauncher(null);

            Process.GetCurrentProcess().Kill();
        }
    }
}
