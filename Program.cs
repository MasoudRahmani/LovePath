using System;
using System.Diagnostics;
using System.Security;
using LovePath.Util;
using System.Threading.Tasks;
using System.Threading;
using System.Security.AccessControl;
using System.Collections.Generic;

namespace LovePath
{
    class Program
    {
        static Mutex mutex = new Mutex(true, "Lovely Path Explorer - Masoud Dono for healp - :)");
        private protected static SecureString _Securepasword;

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
            {
                ShowExit(w.Message);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

        }

        static void Start()
        {
            bool OnetimeRun = true;
            bool wrongpassword = false;
            var authorizedUsers = new List<string>();
            var machineDefaultAccount = Utils.GetWellKnownSidsName();

            var cnf = new Config();
            while (true)// Exist only when Z is pressed
            {
                try
                {
                    if (Console.ReadKey().Key == ConsoleKey.F1)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.Escape)
                        {
                            if (OnetimeRun) // After Secret Entrance, We try to read config file or create it. Only once
                            {
                                cnf.Init();
                                OnetimeRun = false;
                                wrongpassword = !cnf.UseInitialPassword; //we get password to read config or create it -- so it is ture if folder and config share user
                                try
                                {
                                    var rules = Utils.GetFileAccessRule(cnf.LovePath);
                                    foreach (AuthorizationRule rule in rules)
                                    {
                                        var found = machineDefaultAccount.Find(x => x.ToLowerInvariant().Contains(rule.IdentityReference.Value.ToLowerInvariant()));
                                        //Not contain
                                        if (string.IsNullOrWhiteSpace(found))
                                            authorizedUsers.Add(rule.IdentityReference.ToString());
                                    }
                                }
                                catch (Exception w)
                                {
                                    Console.WriteLine(
                                        "Common: File/Folder has no readable access.\n\t" +
                                        @"IT IS GOOD, Only config should have <read permission> access." +
                                        "\n--- Error:" + w.Message);
                                }
                            }
                            if (wrongpassword)
                            {
                                if (authorizedUsers.Count == 0) Console.WriteLine("Permission to read users is denied. Guess Correct User on folder");
                                else if (authorizedUsers.Count == 1)
                                    Console.WriteLine($"User of VALID LovePath: {authorizedUsers[0]}");
                                else
                                    Console.WriteLine(
                                        $"LovePath NOT valid OR Changed Permission.\n" +
                                        $"Try thses Users:\n" +
                                        $"\t{string.Join("\n\t", authorizedUsers) }"
                                        );

                                cnf.ChangePassword();
                            }

                            _Securepasword = Utils.ConvertToSecurePass(cnf.Password);

                            var result = RunasProcess_API(cnf.ExplorerFullPath, cnf.LovePath, cnf.User, _Securepasword);
                            if (result) break; //exit
                            else
                            {
                                Console.Write("Failed!!");
                                wrongpassword = true;
                            }
                        }
                        else ShowExit();
                    }
                    else ShowExit();
                }
                catch (Exception e)
                {
                    ShowExit(e.Message);
                }
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

        private static bool RunasProcess_API(string filename, string arg, string user_noDomain, SecureString pass)
        {
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
                        UserName = user_noDomain,
                        Password = pass,
                        LoadUserProfile = true

                    };
                    cmd.StartInfo = startInfo;
                    cmd.Start();

                    //cmd.WaitForExit();

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

        private static void ShowExit(string msg = "")
        {
            Console.Clear();
            var newline = string.IsNullOrWhiteSpace(msg) ? "" : "\n";
            Console.WriteLine($@"{msg}{newline} Press Z to exit...");
            var pressedkey = Console.ReadKey();
            if (pressedkey.Key == ConsoleKey.Z) Environment.Exit(0);
            Console.WriteLine("  Start...");
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        //var result = await RunasProcess_Shell(explorer, love, domain + @"\\" + user); //Dont know how to read consoloe output since process is also working on console (get password)
        //if (result) ConsoleUtils.MinizeConsole();
        private async static Task<bool> RunasProcess_Shell(string explorer, string arg, string userwithdomain)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
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
                        Arguments = $"/c runas /profile /user:{userwithdomain} " + $"\"{explorer} {arg}\"",
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
    }


}
