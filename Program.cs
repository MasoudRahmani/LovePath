using LovePath.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
                TryToGetLovePathAccessCredential(cnf);
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

        private static void TryToGetLovePathAccessCredential(Config config)
        {
            try
            {
                List<string> authorizedUsers = SecurityUtil.GetUsersWithAccessOnFile(config.LovePath);

                if (authorizedUsers.Count == 0)
                {
                    Console.WriteLine("\n\tNo human account with Access on path.\n");
                    config.GetPassword(" ? ? ? ");
                }
                else
                {
                    for (int i = 0; i < authorizedUsers.Count; i++)
                        Console.WriteLine($"{i}. {authorizedUsers[i]}");
                    int choice = 0;

                    bool idiot = true;
                    while (idiot & authorizedUsers.Count > 1)
                    {
                        Console.Write($"Choose User [0-{authorizedUsers.Count - 1}]: ");
                        var input = Console.ReadLine();

                        idiot = !int.TryParse(input, out choice);
                        if (idiot) Console.WriteLine("Wrong, Again: ");
                    }

                    var cFullAccount = authorizedUsers[choice].Split('\\');
                    config.Domain = cFullAccount[0];
                    config.User = cFullAccount[1];

                    config.GetPassword(authorizedUsers[choice]);
                }
            }
            catch (Exception w)
            {
                Console.WriteLine(w.Message);
                config.GetPassword(" ? ? ? ");
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