using System;
using LovePath.Utility;
using System.Threading;
using System.Collections.Generic;
using System.IO;

//bug of saving config and reading config ( with encryption enabled )

namespace LovePath
{
    class Program
    {
        static Mutex mutex = new Mutex(true, "LovePath Explorer, Masoud Dono in redemption");

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                //Wait x seconds – if a instance of the program is shutting down.
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
            if (!File.Exists(cnf.ExplorerFullPath))
                Util.ShowExit(
                    "Explorer Not found!\nPlace Explorer beside main program.\n" +
                    "If you changed xplorer name, change config too.");

            if (cnf.UseInitialPassword == false)
            {
                //ShowLovePathPossibleUsers(cnf.LovePath);
                cnf.GetPassword(cnf.User);
            }

            try
            {
                var result = RunUtil.RunasProcess_API(cnf.ExplorerFullPath, cnf.LovePath, cnf.User, cnf.SecurePassword);

                if (result) return;
                else Util.ShowExit("Process Failed to Start!!");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("password"))
                {
                    cnf.UseInitialPassword = false;
                    Start(cnf);
                }
                Util.ShowExit(e.Message);
            }
        }

        private static void ShowLovePathPossibleUsers(string LovePath)
        {
            try
            {
                List<string> authorizedUsers = SecurityUtil.GetUsersWithAccessOnFile(LovePath);

                if (authorizedUsers.Count == 0)
                {
                    Console.WriteLine("No human account on path.");
                }
                else if (authorizedUsers.Count == 1)
                {
                    Console.WriteLine($"Possible Valid User: {authorizedUsers[0]}");
                }
                else
                    Console.WriteLine(
                        $"Path NOT valid OR Changed Permission.\n" +
                        $"Possible Users:\n" +
                        $"\t{string.Join("\n\t", authorizedUsers) }"
                        );
            }
            catch (Exception w)
            {
                Console.WriteLine(
                    "Common: File/Folder has no readable access. Good!\n" +
                    "\n--- Error:" + w.Message);
            }

            //TODO:
            //Console.WriteLine("Change User");
            //get user
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