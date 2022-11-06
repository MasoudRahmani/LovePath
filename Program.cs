using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security;
using System.Reflection;
using System.IO;

namespace LovePath
{
    class Program
    {
        static void Main(string[] args)
        {
            string user = "Hidden";
            string pass;
            var securePass = new SecureString();
            Console.SetWindowSize(53, 8);
            Console.Title = "LovePath";

            Console.BackgroundColor = ConsoleColor.Red;
            while (true)
            {
                var pressedkey = Console.ReadKey();
                if (pressedkey.Key == ConsoleKey.Z) break;

                if (pressedkey.Key == ConsoleKey.F1)
                {
                    Console.Clear();
                    pressedkey = Console.ReadKey();
                    if (pressedkey.Key == ConsoleKey.Z) break;

                    if (pressedkey.Key == ConsoleKey.Escape)
                    {
                        Console.Clear();
                        Console.Write("Password: ");
                        pass = GetInputPassword();//GetHiddenConsoleInput();
                        foreach (var item in pass)
                        {
                            securePass.AppendChar(item);
                        }

                        using (Process cmd = new Process())
                        {
                            try
                            {
                                //var xplorePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"XYplorerPortable.exe");
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    //Verb="runas", //makes his run as admin
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    CreateNoWindow = true,
                                    Arguments = "/c" + @"XYplorerPortable.exe",
                                    WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory,
                                    //RedirectStandardInput = true,
                                    //RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    UserName = user,
                                    Password = securePass
                                };
                                cmd.StartInfo = startInfo;
                                cmd.Start();
                            }
                            catch (Exception w)
                            {
                                Console.WriteLine(w.Message);
                            }

                            //cmd.StandardInput.WriteLine(notepad);
                            //cmd.StandardInput.Flush();
                            //cmd.StandardInput.Close();
                            //cmd.WaitForExit();
                            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                        }
                    }
                    else { Console.Clear(); Console.WriteLine(" ---2--- Press Z to exit..."); }
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine(" ---1--- Press Z to exit...");
                }
            }
        }

        private static string GetHiddenConsoleInput()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace && input.Length > 0) input.Remove(input.Length - 1, 1);
                else if (key.Key != ConsoleKey.Backspace) input.Append(key.KeyChar);
            }
            return input.ToString();
        }
        public static string GetInputPassword()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                int x = Console.CursorLeft;
                int y = Console.CursorTop;
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                    Console.SetCursorPosition(x - 1, y);
                    Console.Write(" ");
                    Console.SetCursorPosition(x - 1, y);
                }
                else if (key.KeyChar < 32 || key.KeyChar > 126)
                {
                    Trace.WriteLine("Output suppressed: no key char"); //catch non-printable chars, e.g F1, CursorUp and so ...
                }
                else if (key.Key != ConsoleKey.Backspace)
                {
                    input.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return input.ToString();
        }
    }
}
