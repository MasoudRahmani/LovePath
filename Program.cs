using System;
using System.Text;
using System.Diagnostics;
using System.Security;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace LovePath
{
    class Program
    {
        /// <summary>
        /// center console class
        /// </summary>
        public static class ConsoleUtils
        {

            public static void CenterConsole()
            {
                IntPtr hWin = GetConsoleWindow();
                RECT rc;
                GetWindowRect(hWin, out rc);
                Screen scr = Screen.FromPoint(new Point(rc.left, rc.top));
                int x = scr.WorkingArea.Left + (scr.WorkingArea.Width - (rc.right - rc.left)) / 2;
                int y = scr.WorkingArea.Top + (scr.WorkingArea.Height - (rc.bottom - rc.top)) / 2;
                MoveWindow(hWin, x, y, rc.right - rc.left, rc.bottom - rc.top, false);
            }
            public static void MinizeConsole()
            {
                IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

                ShowWindow(handle, 6);
            }

            // P/Invoke declarations
            private struct RECT { public int left, top, right, bottom; }
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr GetConsoleWindow();
            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool GetWindowRect(IntPtr hWnd, out RECT rc);
            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);

            /// <summary>
            /// Minimize Console
            /// </summary>
            [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ShowWindow([In] IntPtr hWnd, [In] int nCmdShow);
        }




        static void Main(string[] args)
        {
            ConsoleUtils.CenterConsole();
            string user = "Hidden";
            string pass;
            var securePass = new SecureString();


            string explorer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XYplorerPortable.exe");
            //string explorer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Explorer++.exe");
            //string explorer = @"C:\Windows\explorer.exe";
            string love = @"E:\Temp\MISC\personal\Death Note 01.jpg\";


            if (!File.Exists(explorer)) Console.WriteLine("Explorer Not found!");

            SetConsoleSettings();

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
                        RunasProcess_Shell(explorer, love, user);
                        ConsoleUtils.MinizeConsole();
                    }
                    else ShowExit();
                }
                else ShowExit();
            }
        }

        private static void SetConsoleSettings()
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.SetWindowSize(70, 10);
            Console.SetBufferSize(70, 10);//no scrollbar
            Console.Title = "LovePath";
            Console.Clear();
        }

        private static void ShowExit()
        {
            Console.Clear();
            Console.WriteLine("  Press Z to exit...");
        }
        private static void RunasProcess_Shell(string explorer, string arg, string user)
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

        /* ----------------  depricated --------------------------  */
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
