using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace LovePath.Util
{
    public static class MySerialization
    {

        public static string Serialize_notFormatted<T>(T Obj)
        {
            using (var ms = new MemoryStream())
            {
                DataContractJsonSerializer serialiser = new DataContractJsonSerializer(typeof(T));
                serialiser.WriteObject(ms, Obj);
                byte[] json = ms.ToArray();
                return Encoding.UTF8.GetString(json, 0, json.Length);
            }
        }
        public static T Deserialize_notFormatted<T>(string Json)
        {
            Json = Json.Replace(@"\\", @"\").Replace(@"\", @"\\");
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(Json)))
            {
                DataContractJsonSerializer serialiser = new DataContractJsonSerializer(typeof(T));
                var deserializedObj = (T)serialiser.ReadObject(ms);
                return deserializedObj;
            }
        }

        public static string Serialize<T>(T Obj)
        {
            /* Too many dll
            //var options = new JsonSerializerOptions()
            //{
            //    WriteIndented = true
            //};
            //return System.Text.Json.JsonSerializer.Serialize<T>(Obj, options); */
            return FormatJson(Serialize_notFormatted<T>(Obj));
        }

        public static T Deserialize<T>(string Json)
        {
            /* Too many dll //return System.Text.Json.JsonSerializer.Deserialize<T>(Json);*/
            return Deserialize_notFormatted<T>(Json);
        }

        private const string INDENT_STRING = "    ";
        static string FormatJson(string json)
        {

            int indentation = 0;
            int quoteCount = 0;
            var result =
                from ch in json
                let quotes = ch == '"' ? quoteCount++ : quoteCount
                let lineBreak = ch == ',' && quotes % 2 == 0 ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, indentation)) : null
                let openChar = ch == '{' || ch == '[' ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, ++indentation)) : ch.ToString()
                let closeChar = ch == '}' || ch == ']' ? Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, --indentation)) + ch : ch.ToString()
                select lineBreak == null
                            ? openChar.Length > 1
                                ? openChar
                                : closeChar
                            : lineBreak;

            return String.Concat(result);
        }
    }

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

        // P/Invoke declarations Center
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

    public static class Utils
    {
        public static AuthorizationRuleCollection GetFileAccessRule(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            FileSecurity fs = fi.GetAccessControl();

            return fs.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
        }
        public static void WriteFileSecurely(string path, string data, string user, bool encrypted = false)
        {
            using (FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, (encrypted) ? FileOptions.Encrypted : FileOptions.None))
            {
                var byt = Encoding.UTF8.GetBytes(data);
                fs.Write(byt, 0, byt.Length);
            }

            Clear_SetFileSecurity(path, user);
        }

        /// <summary>
        /// Remove all Permission of a file or folder and set only the given user
        /// https://stackoverflow.com/questions/18740860/remove-all-default-file-permissions
        /// </summary>
        /// <param name="filePath"> file or directory</param>
        /// <param name="domainName">domain, if empty current domain</param>
        /// <param name="userName">which user to have full control</param>
        public static void Clear_SetFileSecurity(string filePath, string userName, string domainName = "")
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                domainName = Environment.UserDomainName;
            }

            FileInfo fi = new FileInfo(filePath);//get file info
            FileSecurity fs = fi.GetAccessControl();//get security access

            //remove any inherited access
            fs.SetAccessRuleProtection(true, false);

            //get any special user access
            AuthorizationRuleCollection rules = fs.GetAccessRules(true, true, typeof(NTAccount));

            //remove any special access
            foreach (FileSystemAccessRule rule in rules)
                fs.RemoveAccessRule(rule);

            //add current user with full control.
            fs.AddAccessRule(new FileSystemAccessRule(domainName + "\\" + userName, FileSystemRights.FullControl, AccessControlType.Allow));
            var builtin_user = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            fs.AddAccessRule(new FileSystemAccessRule(builtin_user, FileSystemRights.ReadPermissions, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            //add all other users delete only permissions.
            //fs.AddAccessRule(new FileSystemAccessRule("Authenticated Users", FileSystemRights.Delete, AccessControlType.Allow));

            //flush security access.
            File.SetAccessControl(filePath, fs);
        }


        public static string GetHiddenConsoleInput()
        {
            Console.Write("Password: ");
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
            Console.Write("Password: ");
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

        public static List<string> GetMachineUsers()
        {
            var localUsers = new List<string>();
            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject envVar in searcher.Get())
            {
                localUsers.Add((string)envVar["Name"]);
            }
            return localUsers;
        }
        public static List<string> GetWellKnownSidsName()
        {
            List<string> sids = new List<string>();

            SecurityIdentifier sid;
            string sidName;
            foreach (WellKnownSidType sidType in Enum.GetValues(typeof(WellKnownSidType)))
            {
                try
                {
                    sid = new SecurityIdentifier(sidType, null);
                }
                catch
                {
                    Debug.WriteLine("failed to create: " + sidType.ToString());
                    continue;
                }
                sidName = TranslateSid(sid);

                if (string.IsNullOrEmpty(sidName) == false)
                    sids.Add(sidName);
            }
            return sids;
        }

        public static bool AccountExists(string name)
        {
            bool bRet = false;
            try
            {
                NTAccount acct = new NTAccount(name);
                SecurityIdentifier id = (SecurityIdentifier)acct.Translate(typeof(SecurityIdentifier));

                bRet = id.IsAccountSid();
            }
            catch (IdentityNotMappedException)
            {
                //
            }
            return bRet;
        }
        public static string TranslateSid(SecurityIdentifier sid)
        {
            string sidName = string.Empty;
            try
            {
                sidName = sid.Translate(typeof(NTAccount)).ToString();
            }
            catch
            {
                Debug.WriteLine("failed to translate: " + sid.ToString());
            }
            return sidName;
        }
        public static SecureString ConvertToSecurePass(string pass)
        {
            var securePass = new SecureString();

            securePass.Clear();
            foreach (var item in pass)//GetHiddenConsoleInput();
            {
                securePass.AppendChar(item);
            }
            return securePass;
        }


    }

}
