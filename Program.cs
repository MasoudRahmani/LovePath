using System;
using System.Text;
using System.Diagnostics;
using System.Security;
using System.IO;
using LovePath.Util;

namespace LovePath
{
    class Program
    {
        static void Main(string[] args)
        {
            var cnf = new Config();
            string configFilePath = Path.Combine(cnf.ProgramPath, cnf.ConfigFileName);
            if (!File.Exists(configFilePath))
            {
                var json = MySerialization.Serialize<Config>(cnf);
                using (var sw = new StreamWriter(configFilePath, false, Encoding.UTF8))
                    sw.WriteLine(json);
            }

            SetConsoleSettings();

            string user = cnf.User;
            string explorer = Path.Combine(cnf.ProgramPath, cnf.XplorerName);
            string love = cnf.LovePath;

            if (!File.Exists(explorer)) Console.WriteLine("Explorer Not found!");

            while (true)
            {
                var pressedkey = Console.ReadKey();
                if (pressedkey.Key == ConsoleKey.Z) break;

                if (pressedkey.Key == ConsoleKey.F1)
                {
                    pressedkey = Console.ReadKey();
                    if (pressedkey.Key == ConsoleKey.Z) break;

                    if (pressedkey.Key == ConsoleKey.Escape)
                    {
                        Console.Clear();
                        var result = RunasProcess_Shell(explorer, love, user);
                        if (result) ConsoleUtils.MinizeConsole();
                    }
                    else ShowExit();
                }
                else ShowExit();
            }
        }

        private static void SetConsoleSettings()
        {
            ConsoleUtils.CenterConsole();

            Console.Title = "LovePath";
            Console.BackgroundColor = ConsoleColor.Red;
            Console.SetWindowSize(70, 10);
            Console.SetBufferSize(70, 10);//no scrollbar

            Console.Clear();
        }

        private static bool RunasProcess_Shell(string explorer, string arg, string user)
        {
            using (Process cmd = new Process())
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = false,
                        Arguments = $"/c runas /profile /user:{Environment.UserDomainName}\\{user} " + $"\"{explorer} {arg}\"",
                        //RedirectStandardInput = true,
                        //RedirectStandardOutput = true,
                        UseShellExecute = false,
                        LoadUserProfile = true,
                        ErrorDialog = true

                    };
                    cmd.StartInfo = startInfo;
                    cmd.Start();

                    cmd.WaitForExit();

                    return true;
                }
                catch (Exception w)
                {
                    if (w.Message.Contains("The directory name is invalid"))
                    { Console.WriteLine("Hidden User doesnt have access to explorer application.\n change application installation folder to some place accessible"); }
                    else
                    {
                        Console.WriteLine(w.Message);
                    }
                    return false;
                }
            }
        }

        private static void ShowExit()
        {
            Console.Clear();
            Console.WriteLine("  Press Z to exit...");
        }
        /* ----------------  depricated --------------------------  */
        //string pass;
        //var securePass = new SecureString();
        //Console.Write("Password: ");
        //pass = GetInputPassword();//GetHiddenConsoleInput();
        //securePass.Clear();
        //foreach (var item in pass)
        //{
        //    securePass.AppendChar(item);
        //}
        //RunasProcess_API(explorer, love, user, securePass);
        private static void RunasProcess_API(string filename, string arg, string user, SecureString pass)
        {
            using (Process cmd = new Process())
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = filename,
                        //Verb = "runas", //makes his run as admin if user and pass not there
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        Arguments = arg,
                        UseShellExecute = false,
                        UserName = user,
                        Password = pass,
                        LoadUserProfile = true,
                        ErrorDialog = true
                    };
                    cmd.StartInfo = startInfo;
                    cmd.Start();
                    cmd.WaitForExit();
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
