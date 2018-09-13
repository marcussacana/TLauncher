using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TLIB;
using static TLauncher.Common;
using static TLauncher.Unmanaged;

namespace TLauncher {
    internal static class Replacer {
        internal static bool MTL = false;
        internal static string From = null;
        internal static string To = null;
        internal static bool Invalidate = false;
        internal static Dictionary<string, string> Database = new Dictionary<string, string>();
        internal static void InitializeHook(string[] Args) {
            Console.WriteLine("Initializing...");

            Config Setup = new Config();
            if (!MTL && System.IO.File.Exists(Common.Setup)) {
                StructReader Reader = new StructReader(Common.Setup);
                
                Reader.ReadStruct(ref Setup);
                Reader.Close();

                if (Setup.TLs.LongLength - Setup.Strings.LongLength > 1)
                    throw new Exception("Bad Configuration, The String and Tl Length missmatch.");

                for (long i = 0; i < Setup.Strings.LongLength; i++)
                    Database.Add(Setup.Strings[i], Setup.TLs[i]);
            } else {
                Setup = new Config() {
                    Delay = 100,
                    Invalidate = 0,
                    Executable = Args.First()
                };
                Args = new string[0];
            }

            Invalidate = Setup.Invalidate > 0;

            ProgProc = new Process() {
                StartInfo = new ProcessStartInfo() {
                    Arguments = ParseArguments(Args),
                    FileName = AppDomain.CurrentDomain.BaseDirectory + Setup.Executable,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                }
            };

            Console.WriteLine("Starting Process...");
            ProgProc.Start();

            Console.WriteLine("Wainting Main Window Open...");
            while (ProgProc.MainWindowHandle == IntPtr.Zero) 
                System.Threading.Thread.Sleep(100);

            Console.WriteLine("Initializing...");
            while (!ProgProc.HasExited) {
                System.Threading.Thread.Sleep(Setup.Delay);

                var CB = new CallBack(Replace);
                EnumWindows(CB, 0);

                IntPtr Handler = GetMenu(ProgProc.MainWindowHandle);
                ReplaceMenu(Handler);
            }
        }
        private static bool Replace(IntPtr Handler, int Paramters) {
            int Len = GetWindowTextLength(Handler);
            StringBuilder sb = new StringBuilder(Len + 1);
            GetWindowText(Handler, sb, sb.Capacity);
            string Text = sb.ToString();
            GetWindowThreadProcessId(Handler, out uint PID);

            if (PID == ProgProc.Id) {
                var CB = new CallBack(Replace);
                EnumChildWindows(Handler, CB, IntPtr.Zero);
            }
            if (PID != ProgProc.Id)
                return true;

            if (!Database.ContainsKey(Text)) {
                if (!MTL)
                    return true;
                else {
                    try {
                        string TL = Google.Translate(Text, From, To);
                        if (TL == Text || string.IsNullOrWhiteSpace(TL))
                            return true;
                        Database[Text] = TL;
                    } catch {
                        return true;
                    }
                }
            }

            string Translation = Database[Text];

            HandleRef href = new HandleRef(null, Handler);
            SendMessage(href, WM_SETTEXT, IntPtr.Zero, Translation);

            if (Invalidate)
                RedrawWindow(Handler, IntPtr.Zero, IntPtr.Zero, RDW_ERASE | RDW_INVALIDATE);

#if DEBUG
            Console.WriteLine("Text Translated from \"{0}\" to \"{1}\"", Text, Translation);
#endif
            return true;
        }
        private static void ReplaceMenu(IntPtr Handler) {
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
                    ReplaceMenu(MenuInfo.hSubMenu);

                if (!Sucess)
                    continue;

                if (!Database.ContainsKey(Text)) {
                    if (!MTL)
                        continue;
                    else {
                        try {
                            string TL = Google.Translate(Text, From, To);
                            if (TL == Text || string.IsNullOrWhiteSpace(TL))
                                continue;
                            Database[Text] = TL;
                        } catch {
                            continue;
                        }
                    }
                }

                string Translation = Database[Text];

                MenuInfo.dwTypeData = Translation;

                Sucess = SetMenuItemInfo(Handler, i, true, ref MenuInfo);

                if (Invalidate)
                    RedrawWindow(Handler, IntPtr.Zero, IntPtr.Zero, RDW_ERASE | RDW_INVALIDATE);
#if DEBUG
                if (Sucess)
                    Console.WriteLine("Text Translated: {0}", Text);
#endif
            }
        }
    }
}
