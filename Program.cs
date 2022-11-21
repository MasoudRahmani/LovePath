using System;
using LovePath.Utility;
using System.Threading;
using System.Security.AccessControl;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

//bug of saving config and reading config ( with encryption enabled )
// how to to read secret enterance from config

namespace LovePath
{
    class Program
    {
        static Mutex mutex = new Mutex(true, "Lovely Path Explorer - Masoud Dono for redemption - :)");

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Wait x seconds – if a instance of the program is shutting down.
                if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                {
                    Console.WriteLine("Another instance of the app is running. Bye!\n Press a key to exit...");
                    Console.ReadKey();
                    return;
                }
                SetConsoleSettings();

                OpenSasemi();
            }
            catch (Exception w)
            {
                Util.ShowExit(w.Message);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public static void OpenSasemi()
        {
            var cnf = new Config();

            cnf.WaitForMagicWord();

            cnf.Initiate();

            Start(cnf);
        }

        private static void Start(Config cnf)
        {
            List<string> userWithAccess = GetLovePathValidAccessUsers(cnf.LovePath);

            if (userWithAccess == null) System.Diagnostics.Debug.WriteLine("Love Path Access is Good");// "Good" 

            if (cnf.UseInitialPassword == false)
                cnf.GetPassword();

            try
            {
                var result = RunUtil.RunasProcess_API(cnf.ExplorerFullPath, cnf.LovePath, cnf.User, cnf.SecurePassword);

                if (result) Utility.Util.ShowExit("Process Started!");
                else Utility.Util.ShowExit("Process Failed to Start!!");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("password"))
                    cnf.UseInitialPassword = false;
                Utility.Util.ShowExit(e.Message);
            }
        }

        private static List<string> GetLovePathValidAccessUsers(string LovePath)
        {
            var authorizedUsers = new List<string>();
            var machineDefaultAccount = SecurityUtil.GetWellKnownSidsName();

            try
            {
                var rules = SecurityUtil.GetFileAccessRule(LovePath);
                foreach (AuthorizationRule rule in rules)
                {
                    var found = machineDefaultAccount.Find(x => x.ToLowerInvariant().Contains(rule.IdentityReference.Value.ToLowerInvariant()));
                    //Not contain
                    if (string.IsNullOrWhiteSpace(found))
                        authorizedUsers.Add(rule.IdentityReference.ToString());
                }

                if (authorizedUsers.Count == 0)
                {
                    Console.WriteLine("Permission to read Access Control is denied. Guess Correct User.");
                }
                else if (authorizedUsers.Count == 1)
                {
                    Console.WriteLine($"LovePath VALID User: {authorizedUsers[0]}");
                }
                else
                    Console.WriteLine(
                        $"Love Path NOT valid OR Changed Permission.\n" +
                        $"Possible Users:\n" +
                        $"\t{string.Join("\n\t", authorizedUsers) }"
                        );
                return authorizedUsers;
            }
            catch (Exception w)
            {
                Console.WriteLine(
                    "Common: File/Folder has no readable access.\n\t" +
                    @"IT IS GOOD, Only config should have <read permission> access." +
                    "\n--- Error:" + w.Message);
                return null;
            }
        }

        private static void SetConsoleSettings()
        {
            ConsoleUtil.CenterConsole();

            Console.Title = "LovePath";
            Console.BackgroundColor = ConsoleColor.Red;
            Console.SetWindowSize(70, 10);
            Console.SetBufferSize(70, 10);//no scrollbar

            Console.Clear();
        }




    }


}
