using System;
using System.Text;
using System.Diagnostics;
using System.Security;
using System.IO;
using LovePath.Util;
using System.Threading.Tasks;
using System.Threading;

//todo: check return result of runas process then minimize

namespace LovePath
{
    class Program
    {
        static Mutex mutex = new Mutex(true, "Lovely Path Explorer - Masoud Dono for healp - :)");
        [STAThread]
        static void Main(string[] args)
        {
            SetConsoleSettings();

            // Wait x seconds – if a instance of the program is shutting down.
            if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
            {
                Console.WriteLine("Another instance of the app is running. Bye!\n Press a key to exit...");
                Console.ReadKey();
                return;
            }
            try
            {
                Start();
            }
            catch (Exception w)
            { Console.WriteLine(w.Message); }
            finally
            {
                mutex.ReleaseMutex();
            }

        }
        static async void Start()
        {
            var cnf = new Config();
            string configFilePath = Path.Combine(cnf.ProgramPath, cnf.ConfigFileName);
            if (!File.Exists(configFilePath))
            {
                var json = MySerialization.Serialize<Config>(cnf);
                using (var sw = new StreamWriter(configFilePath, false, Encoding.UTF8))
                    sw.WriteLine(json);
            }
            else
            {
                cnf = MySerialization.Deserialize<Config>(new StreamReader(configFilePath).ReadToEnd());
            }

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
                        //var result = await RunasProcess_Shell(explorer, love, user); //Dont know how to read consoloe output since process is also working on console (get password)
                        var result = RunasProcess_API(explorer, love, user);
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

        private async static Task<bool> RunasProcess_Shell(string explorer, string arg, string user)
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
                        //RedirectStandardError = true,
                        UseShellExecute = false,
                        LoadUserProfile = true
                    };
                    cmd.StartInfo = startInfo;

                    cmd.Start();

                    cmd.WaitForExit();

                    return true;
                }
                catch (Exception w)
                {
                    Console.WriteLine(w.Message);
                    return false;
                }
            }
        }

        private static bool RunasProcess_API(string filename, string arg, string user)
        {
            var pass = GetUserPass();
            using (Process cmd = new Process())
            {
                
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = filename,
                        //WindowStyle = ProcessWindowStyle.Hidden,
                        //CreateNoWindow = true,
                        Arguments = $"\"{arg.Replace(@"\\", @"\")}\"",
                        RedirectStandardOutput = true,
                        //RedirectStandardError = true,
                        UseShellExecute = false,
                        UserName = user,
                        Password = pass,
                        LoadUserProfile = true

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

        private static SecureString GetUserPass()
        {
            Console.Write("Password: ");

            var securePass = new SecureString();

            securePass.Clear();
            foreach (var item in GetInputPassword())//GetHiddenConsoleInput();
            {
                securePass.AppendChar(item);
            }
            return securePass;
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
