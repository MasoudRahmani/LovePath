using System;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Text;

namespace LovePath.Util
{

    public static class Utils
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

        public static void WriteFile(string path, string data)
        {
            var fs = File.Create(path);
            using (fs)
            {
                var byt = Encoding.UTF8.GetBytes(data);
                fs.Write(byt, 0, byt.Length);
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


        public static void ShowExit(string msg = "")
        {
            Console.Clear();
            var newline = string.IsNullOrWhiteSpace(msg) ? string.Empty : Environment.NewLine;

            Console.Write($@"{msg}{newline} Z-> Exit, Y-> Restart: ");

            var key = Console.ReadKey().Key;
            if (key == ConsoleKey.Z) Environment.Exit(0);
            if (key == ConsoleKey.Y) Restart();

            Console.Clear();
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
        private static object GetClassInstanceOfTypeName(string strFullyQualifiedName, ref Type objType)
        {/* Something wrong*/
            //this
            System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(strFullyQualifiedName);

            //or this
            Type type = Type.GetType("LovePath." + strFullyQualifiedName);
            objType = type;
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }

            return null;
        }
    }

}
