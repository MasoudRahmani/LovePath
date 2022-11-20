using System;
using System.Diagnostics;
using System.Security;

namespace LovePath
{
    static class Run
    {

        public static bool RunasProcess_API(string filename, string arg, string user_noDomain, SecureString pass)
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
                    return cmd.Start();

                    //cmd.WaitForExit();
                }
                catch (Exception w)
                {
                    throw w;
                }
            }
        }


        //if (result) ConsoleUtils.MinizeConsole();
        public static bool RunasProcess_Shell(string command, string arg, string userwithdomain)
        {
            using (Process cmd = new Process())
            {
                string fullcmd = $"\"{command} \"{@arg}\"\"";
                string final = $"/c runas /profile /user:{userwithdomain} " + fullcmd;
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = false,
                        Arguments = final,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
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
