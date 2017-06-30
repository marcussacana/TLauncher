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
            Console.WriteLine("Write the Executable Name:");
            string Exe = Console.ReadLine();

            Console.WriteLine("Write delay to find for new text in ms:");
            int Delay = int.Parse(Console.ReadLine());

            Console.WriteLine("Initializing Executable...");
            ProgProc = new Process() {
                StartInfo = new ProcessStartInfo() {
                    Arguments = ParseArguments(Args),
                    FileName = AppDomain.CurrentDomain.BaseDirectory + Exe
                }
            };
            Console.WriteLine("Initializing Hook...");
            HookEnabler();

            Console.WriteLine("Dumping Text...");
            StreamWriter Writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "ProgramText.txt", false, Encoding.Unicode);

            foreach (string str in Texts) {
                string line = str;
                Encode(ref line, true);
                Writer.WriteLine(line);
            }
            Writer.Flush();
            Writer.Close();

            Console.WriteLine("Dumped, Translate all lines and press a key to save the translation.");
            Console.WriteLine("If have any string who you don't want translate, set it to :IGNORE:");
            Console.ReadKey();
            Console.WriteLine("Reading Translation...");

            TextReader Reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "ProgramText.txt", Encoding.Unicode);
            List<string> Tls = new List<string>();

            while (Reader.Peek() != -1) {
                string line = Reader.ReadLine();
                Encode(ref line, false);

                int ID = Tls.Count;
                if (ID == Texts.Count)
                    continue;
                if (string.IsNullOrWhiteSpace(line) || line.Trim().ToLower() == ":ignore:") {
                    Texts.RemoveAt(ID);
                    continue;
                }
                Tls.Add(line);
            }
            Reader.Close();

            Console.WriteLine("Generating Configuration...");

            Config LauncherData = new Config() {
                Signature = "TLLD",
                Executable = Exe,
                Strings = Texts.ToArray(),
                TLs = Tls.ToArray()
            };

            Console.WriteLine("Saving Configuration...");

            StructWriter Output = new StructWriter(Setup, false, Encoding.UTF8);
            Output.WriteStruct(ref LauncherData);
            Output.Close();

            Console.WriteLine("Settings Saved, Press a Key to Exit.");
            Console.ReadKey();
        }

        private static void HookEnabler() {
            ProgProc.Start();
            Texts = new List<string>();

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

                if (Texts.Contains(Text) || string.IsNullOrWhiteSpace(Text) || !Sucess)
                    continue;

                Texts.Add(Text);

                Console.WriteLine("Text Hooked: {0}", Text);
            }
        }

        static List<string> Texts;
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
            if (Texts.Contains(Text) || PID != ProgProc.Id)
                return true;

            Texts.Add(Text);

            Console.WriteLine("Text Hooked: {0}", Text);

            return true;
        }

    }
}
