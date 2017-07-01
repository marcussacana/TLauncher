using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static TLauncher.Common;
using static TLauncher.Unmanaged;

namespace TLauncher {
    internal static class Dumper {
        internal static void SetupLauncher(string[] Args) {
            if (Texts.Count == 0) {
                Console.WriteLine("Write the Executable Name:");
                Exe = Console.ReadLine();

                Console.WriteLine("Write delay to find for new text in ms:");
                Delay = int.Parse(Console.ReadLine());

                Console.WriteLine("You Want Invalidate the game window when translate something? Y/N");
                Console.WriteLine("(Can increase the CPU/GPU usage if the window change the text constantly.)");
                Invalidate = Console.ReadKey().KeyChar.ToString().ToUpper()[0] == 'Y';
            }

            Console.WriteLine("Initializing Executable...");
            ProgProc = new Process() {
                StartInfo = new ProcessStartInfo() {
                    Arguments = ParseArguments(Args),
                    FileName = AppDomain.CurrentDomain.BaseDirectory + Exe,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                }
            };

            Console.WriteLine("Initializing Hook...");
            HookEnabler();
            Console.WriteLine("Dumping Text...");

            StreamWriter Writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "ProgramText.txt", false, Encoding.Unicode);

            for (int i = 0; i < Texts.Count(); i++) {
                string line = Tls.Count != 0 && i < Tls.Count ? Tls[i] : Texts[i];

                Encode(ref line, true);
                Writer.WriteLine(line);
            }
            Writer.Flush();
            Writer.Close();

            Console.WriteLine("Dumped, Translate all lines and press a key to save the translation.");
            Console.WriteLine("If have any string who you don't want translate, set it to :IGNORE:");
            Console.ReadKey();
            Console.WriteLine("Reading Translation...");

            Tls = new List<string>();

            TextReader Reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "ProgramText.txt", Encoding.Unicode);

            int ID = -1;
            while (Reader.Peek() != -1) {
                string Line = Reader.ReadLine();
                Encode(ref Line, false);

                if (++ID >= Texts.Count)
                    continue;
                if (string.IsNullOrWhiteSpace(Line) || Line.Trim().ToLower() == ":ignore:") {
                    Texts.RemoveAt(ID--);
                    continue;
                }
                Tls.Add(Line);
            }

            Reader.Close();

            Console.WriteLine("Generating Configuration...");

            Config LauncherData = new Config() {
                Signature = "TLLD",
                Executable = Exe,
                Strings = Texts.ToArray(),
                TLs = Tls.ToArray(),
                Delay = Delay,
                Invalidate = (byte)(Invalidate ? 1 : 0)
            };

            Console.WriteLine("Saving Configuration...");

            StructWriter Output = new StructWriter(Setup);
            Output.WriteStruct(ref LauncherData);
            Output.Close();

            Console.WriteLine("Settings Saved, Press a Key to Exit.");
            Console.ReadKey();
        }

        private static void HookEnabler() {
            ProgProc.Start();

            Console.WriteLine("Waiting Main Window Open...");

            while (ProgProc.MainWindowHandle == IntPtr.Zero)
                System.Threading.Thread.Sleep(100);

            Console.WriteLine("Hooking...");

            while (!ProgProc.HasExited) {
                System.Threading.Thread.Sleep(500);

                var CB = new CallBack(Dump);
                EnumWindows(CB, 0);

                IntPtr Handler = GetMenu(ProgProc.MainWindowHandle);
                DumpMenu(Handler);
            }

            Console.WriteLine("Process Exited...");
        }

        private static void DumpMenu(IntPtr Handler) {
            int MenuCount = GetMenuItemCount(Handler);
            if (MenuCount == -1)
                return;
            var MenuInfo = new MENUITEMINFO();
            for (int i = 0; i < MenuCount; i++) {
                MenuInfo = new MENUITEMINFO() {
                    cbSize = MENUITEMINFO.SizeOf,
                    fMask = MIIM_STRING | MIIM_SUBMENU,
                    fType = MFT_STRING,
                    dwTypeData = new string(new char[1024]),
                    cch = 1025
                };

                bool Sucess = GetMenuItemInfo(Handler, i, true, ref MenuInfo);

                string Text = MenuInfo.dwTypeData;

                if (MenuInfo.hSubMenu != IntPtr.Zero)
                    DumpMenu(MenuInfo.hSubMenu);

                if (Texts.Contains(Text) || string.IsNullOrWhiteSpace(Text))
                    continue;

                Texts.Add(Text);

                Console.WriteLine("Text Hooked: {0}", Text);
            }
        }

        internal static List<string> Texts = new List<string>();
        internal static string Exe;
        internal static int Delay;
        internal static List<string> Tls = new List<string>();
        internal static bool Invalidate;

        private static bool Dump(IntPtr Handler, int Paramters) {
            int Len = GetWindowTextLength(Handler);
            StringBuilder sb = new StringBuilder(Len + 1);
            GetWindowText(Handler, sb, sb.Capacity);
            string Text = sb.ToString();
            GetWindowThreadProcessId(Handler, out uint PID);

            if (PID == ProgProc.Id) {
                var CB = new CallBack(Dump);
                EnumChildWindows(Handler, CB, IntPtr.Zero);
            }
            if (Texts.Contains(Text) || string.IsNullOrWhiteSpace(Text) || PID != ProgProc.Id)
                return true;

            Texts.Add(Text);

            Console.WriteLine("Text Hooked: {0}", Text);
            return true;
        }

    }
}
