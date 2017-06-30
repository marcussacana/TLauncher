using AdvancedBinary;
using System;
using System.Diagnostics;

namespace TLauncher {
    internal static class Common {
        internal static string Setup => AppDomain.CurrentDomain.BaseDirectory + "TLauncher.dat";
        internal static Process ProgProc;

        internal static string ParseArguments(string[] Args) {
            string CommandLine = string.Empty;
            foreach (string arg in Args) {
                if (arg.Contains(" "))
                    CommandLine += string.Format("\"{0}\"", arg);
                else
                    CommandLine += arg;
                CommandLine += ' ';
            }
            return CommandLine.TrimEnd();
        }
        internal static void Encode(ref string[] Strings, bool Enable) {
            for (int i = 0; i < Strings.Length; i++)
                Encode(ref Strings[i], Enable);
        }

        internal static void Encode(ref string String, bool Enable) {
            if (Enable) {
                string Result = string.Empty;
                foreach (char c in String) {
                    if (c == '\n')
                        Result += "\\n";
                    else if (c == '\\')
                        Result += "\\\\";
                    else if (c == '\t')
                        Result += "\\t";
                    else if (c == '\r')
                        Result += "\\r";
                    else
                        Result += c;
                }
                String = Result;
            } else {
                string Result = string.Empty;
                bool Special = false;
                foreach (char c in String) {
                    if (c == '\\' & !Special) {
                        Special = true;
                        continue;
                    }
                    if (Special) {
                        switch (c.ToString().ToLower()[0]) {
                            case '\\':
                                Result += '\\';
                                break;
                            case 'n':
                                Result += '\n';
                                break;
                            case 't':
                                Result += '\t';
                                break;
                            case 'r':
                                Result += '\r';
                                break;
                            default:
                                throw new Exception("\\" + c + " Isn't a valid string escape.");
                        }
                        Special = false;
                    } else
                        Result += c;
                }
                String = Result;
            }
        }
        internal struct Config {
            [FString(Length = 4)]
            public string Signature;

            public int Delay;

            [CString()]
            public string Executable;

            [PArray(), PString()]
            public string[] Strings;

            [PArray(), PString()]
            public string[] TLs;
        }
    }
}
