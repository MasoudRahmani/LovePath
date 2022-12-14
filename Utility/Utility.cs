using System;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Text;

namespace LovePath.Utility
{

    public static class Util
    {
        public static SecureString ConvertToSecureString(string password)
        {
            var securePass = new SecureString();

            securePass.Clear();
            foreach (var item in password)//GetHiddenConsoleInput();
            {
                securePass.AppendChar(item);
            }
            return securePass;
        }
        // convert a secure string into a normal plain text string
        public static string ConvertToPlainString(this SecureString secureStr)
        {
            string plainStr = new System.Net.NetworkCredential(string.Empty,
                              secureStr).Password;
            return plainStr;
        }

        public static void WriteFile(string path, string data, FileOptions fileOptions)
        {
            /*FileSystemRights -> very imprtant*/
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileSystemRights.FullControl, FileShare.ReadWrite, 4096, fileOptions))
            {
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(data);
                }
            }
        }
        public static void TestTempFile(string data)
        {
            var paths = new string[2] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tmp.tmp_lovepath"),
                "t.tmp"};

            foreach (var path in paths)
            {
                var fs = File.Create(path, 4096);
                using (fs)
                {
                    var byt = Encoding.UTF8.GetBytes(data);
                    fs.Write(byt, 0, byt.Length);

                }
                File.ReadAllText(path);
            }
        }
        public static bool IsWindowsEncrypted(string filepath)
        {
            FileInfo fi = new FileInfo(filepath); //uri is the full path and file name
            return fi.Attributes.HasFlag(FileAttributes.Encrypted);
        }

        public static void ShowExit(string msg = "")
        {
            Console.Clear();
            var newline = string.IsNullOrWhiteSpace(msg) ? string.Empty : Environment.NewLine;

            Console.Write($@"{msg}{newline} Z-> Exit, Y-> Restart: ");

            var key = Console.ReadKey().Key;
            if (key == ConsoleKey.Z) Environment.Exit(0);
            if (key == ConsoleKey.Y) Restart();

            ShowExit();
        }
        private static void Restart()
        {
            Console.WriteLine("Reboot...");
            // Starts a new instance of the program itself
            System.Diagnostics.Process.Start(Path.Combine(Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.FriendlyName));

            // Closes the current process
            Environment.Exit(0);
        }

        private static string help;
        public static void HelpInConsole(ConsoleKey key)
        {
            if (key == ConsoleKey.H)
                help = "h";

            if (help == "h" & key == ConsoleKey.E)
                help += "e";

            if (help == "he" & key == ConsoleKey.L)
                help += "l";

            if (help == "hel" & key == ConsoleKey.P)
                help += "p";

            if (key == ConsoleKey.Help || key == ConsoleKey.End)
                help = "help";

            if (help == "help")
            {
                ShowExit();
                help = "";
            }
        }


        //https://stackoverflow.com/questions/223952/create-an-instance-of-a-class-from-a-string
        public static object GetInstanceofType(string strFullyQualifiedName, ref Type objType)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            objType = type;
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }

            throw new NullReferenceException();
        }
        public static object GetInstanceofType2(string strFullyQualifiedName)
        {
            var result = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(strFullyQualifiedName);
            if (result == null) throw new NullReferenceException();
            return result;
        }
    }

}
