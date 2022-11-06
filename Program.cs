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
            string explorer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XYplorerPortable.exe");
            //string explorer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Explorer++.exe");
            //string explorer = @"C:\Windows\explorer.exe";
            string love = @"E:\Temp\MISC\personal\Death Note 01.jpg\";
            var securePass = new SecureString();

            if (!File.Exists(explorer)) Console.WriteLine("Explorer Not found!");

            Console.BackgroundColor = ConsoleColor.Red;
            Console.SetWindowSize(70, 10);
            Console.Title = "LovePath";
            Console.Clear();

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
                        securePass.Clear();
                        foreach (var item in pass)
                        {
                            securePass.AppendChar(item);
                        }
                        pass = "";
                        using (Process cmd = new Process())
                        {
                            try
                            {
                                //var xplorePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"XYplorerPortable.exe");
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    FileName = explorer,
                                    //Verb = "runas", //makes his run as admin if user and pass not there
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    CreateNoWindow = true,
                                    Arguments = love,
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
                                if (w.Message.Contains("The directory name is invalid"))
                                { Console.WriteLine("Hidden User doesnt have access to explorer application.\n change application installation folder to some place accessible"); }
                                else
                                {
                                    Console.WriteLine(w.Message);
                                }
                            }
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
