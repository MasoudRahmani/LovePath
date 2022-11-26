using LovePath.Impersonation;
using LovePath.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace LovePath
{
    [DataContract]
    public class Config
    {
        #region Fields
        /*-----------------        Used Generaly            --------------- */
        public SecureString SecurePassword;
        public bool UseInitialPassword = false; //If config permission had the same user as user in config file we have the password, otherwise we need to ask again.

        /*-----------------        Used In Property             --------------- */
        private string _programPath = AppDomain.CurrentDomain.BaseDirectory;
        private string _domain = Environment.UserDomainName;
        private string _user = Environment.UserName;
        private string _xplorerName = "XY.exe";
        private string _configFileName = "LoveConfig.json";
        private string _lovePath = Environment.CurrentDirectory;
        private List<ConsoleKey> _secret;
        #endregion

        #region Property

        [DataMember] //If Config file was moved, this is here to check config file and find last path
        public string ProgramPath { get { return _programPath; } private set { _programPath = value; } }
        [DataMember]
        public string Domain
        {
            get { return _domain; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                else
                    _domain = value;
            }
        }
        [DataMember]
        public string User
        {
            get => _user;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException();
                else _user = value;
            }
        }
        [DataMember]
        public string FullAccountName { get { return $"{_domain}\\{_user}"; } private set {; } } // from User
        [DataMember]
        public string XplorerName
        {
            get => _xplorerName;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException();
                if (value == ".") _xplorerName = "Explorer++.exe";
                else
                    _xplorerName = value;
            }
        }
        [DataMember]
        public string ExplorerFullPath { get { return Path.Combine(_programPath, _xplorerName); } private set {; } }
        [DataMember]
        public string LovePath
        {
            get => _lovePath;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException();
                else _lovePath = value;
            }

        }
        [DataMember] //If forget how to login - check config
        public List<ConsoleKey> SecretEntrance
        {
            get
            {
                if (_secret == null)
                {
                    _secret = new List<ConsoleKey>();
                    _secret.Add(ConsoleKey.F1);
                    _secret.Add(ConsoleKey.Escape);
                }
                return _secret;
            }
            set
            {
                if (value == null)
                    _secret = new List<ConsoleKey>();
                else
                    _secret = value;
            }
        }

        //------------------------------
        public string ConfigFileName
        {
            get { return _configFileName; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();
                else _configFileName = value;
            }
        }
        public string ConfigFullPath { get { return Path.Combine(_programPath, _configFileName); } private set {; } }
        #endregion

        /// <summary>
        /// Creates a Config with Pre-Defiend Values, Use Initiate() to Set them correctly.
        /// </summary>
        public Config(string configfilename = "")
        {
            if (!string.IsNullOrWhiteSpace(configfilename)) ConfigFileName = configfilename;
        }

        /// <summary>
        /// Wait until input keys on console matches "SecretEntrance" property.
        /// </summary>
        public void WaitForMagicWord()
        {
            //Secret Entrance
            for (int i = 0; i < SecretEntrance.Count;) //all secret has to be used and be in order
            {
                var key = Console.ReadKey().Key;
                Utility.Util.HelpInConsole(key);
                if (key == SecretEntrance[i]) i++;
                else i = 0;
            }
        }

        /// <summary>
        /// If there is no Config file located at Hardcoded Destination, will create it otherwise read from config.
        /// </summary>
        public void Initiate()
        {
            if (!File.Exists(ConfigFullPath))
            {
                GetAccountInfo();
                GetLovePath();
                GetExplorerName();

                CreateConfig();
            }
            else
            {
                GetConfigFileInfo();
                bool result = ReadConfig();
                if (!result)
                {
                    Console.Write("Reading Config Failed!! Continue with uncertain config? (Y/N): ");
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        Console.WriteLine();
                        return;
                    }
                    else Util.ShowExit("Done.");
                }
            }
        }

        /// <summary>
        /// Ask user for username and password and set/change fields for later use
        /// </summary>
        public void GetAccountInfo()
        {
            Console.Write("(No Domain) Just User:");
            User = Console.ReadLine();

            while (!SecurityUtil.AccountExists(User))
            {
                Console.Write("\nWrong User!, Again :");
                User = Console.ReadLine();
            }

            GetPassword(FullAccountName);
            while (!SecurityUtil.ValidateAccountCredential(User, SecurePassword, Domain))
            {
                Console.WriteLine("\nWrong password!, Again :");
                GetPassword(FullAccountName);
            }
        }

        public void GetPassword(string forUser)
        {
            Console.Write($"\"{forUser}\" User, Enter ");
            var password = ConsoleUtil.GetInputPassword();

            SecurePassword = new SecureString();
            SecurePassword.Clear();
            SecurePassword = Util.ConvertToSecureString(password);
            SecurePassword.MakeReadOnly();
        }

        #region Private Methods

        private void CreateConfig()
        {
            bool done = false;
            ImpersonateUser impersonate = null;

            try
            {
                /* SOLVES Read/Write Encryption Problem */
                _ = RunUtil.RunasProcess_API("cmd.exe", "/c", User, SecurePassword);
                /* SOLVES Read/Write Encryption Problem */

                var json = SerializationUtil.Serialize(this);
                using (impersonate = new ImpersonateUser(ImpersonationType.WinIdentity, Domain, User, SecurePassword))
                {
                    done = impersonate.RunImpersonated(() =>
                     {
                         Util.WriteFile(ConfigFullPath, json, FileOptions.Encrypted); //Does Not Work If i do not use runasProcess_api

                         SecurityUtil.ClearFileAccessRule(ConfigFullPath);

                         SecurityUtil.AllowFileAccessRule(ConfigFullPath, FullAccountName, FileSystemRights.FullControl);
                         SecurityUtil.AllowFileAccessRule(ConfigFullPath, WellKnownSidType.BuiltinUsersSid, FileSystemRights.ReadPermissions);
                     });
                }
                UseInitialPassword = done;

                if (!done) throw new Exception("Create Config File Failed! Open an issue on github. MasoudRahmani/LovePath");
            }
            catch (Exception w)
            {
                if (impersonate != null) impersonate.Dispose();
                throw w;
            }
        }

        private bool ReadConfig()
        {
            bool done = false;
            ImpersonateUser impersonate = null;
            try
            {
                /* SOLVES Read/Write Encryption Problem */
                _ = RunUtil.RunasProcess_API("cmd.exe", "/c", User, SecurePassword);
                /* SOLVES Read/Write Encryption Problem */

                using (impersonate = new ImpersonateUser(ImpersonationType.WinIdentity, Domain, User, SecurePassword))
                {
                    System.Diagnostics.Debug.WriteLine($"Before Impersonation: {Environment.UserName}");
                    done = impersonate.RunImpersonated(() =>
                    {
                        System.Diagnostics.Debug.WriteLine($"After Impersonation: {Environment.UserName}");

                        var json = File.ReadAllText(ConfigFullPath);
                        var configFile_data = SerializationUtil.Deserialize<Config>(json);

                        //If config file user access equals User in config file, We have password.
                        if (configFile_data.User == this.User && configFile_data.Domain == this.Domain)
                            UseInitialPassword = true;

                        Domain = configFile_data.Domain;
                        User = configFile_data.User;

                        XplorerName = configFile_data.XplorerName;
                        LovePath = configFile_data.LovePath;
                    });
                }
                if (!done) UseInitialPassword = false;
                return done;
            }
            catch (Exception w)
            {
                Console.WriteLine(w.Message);
                if (impersonate != null) impersonate.Dispose();
                return false;
            }
        }

        private void GetConfigFileInfo()
        {
            List<string> validUsers = SecurityUtil.GetUsersWithAccessOnFile(ConfigFullPath);

            for (int i = 0; i < validUsers.Count; i++)
                Console.WriteLine($"{i}. {validUsers[i]}");
            int choice = 0;

            bool idiot = true;
            while (idiot & validUsers.Count > 1)
            {
                Console.Write($"Choose User [0-{validUsers.Count - 1}]: ");
                var input = Console.ReadLine();

                idiot = !int.TryParse(input, out choice);
                if (idiot) Console.WriteLine("Wrong, Again: ");
            }

            var cFullAccount = validUsers[choice].Split('\\');
            Domain = cFullAccount[0];
            User = cFullAccount[1];

            GetPassword(validUsers[choice]);
        }

        private void GetExplorerName()
        {
            Console.Write(
                "\n---- Put <Explorer> in Application Directory ----" +
                "\nExplorer Name:");
            XplorerName = @Console.ReadLine();

            while (!File.Exists(ExplorerFullPath))
            {
                Console.Write("\nWrong name!, Agian: ");
                XplorerName = @Console.ReadLine();
            }
        }

        private void GetLovePath()
        {
            Console.Write("\nLovePath:");
            _lovePath = @Console.ReadLine();
            if (_lovePath == ".") _lovePath = Environment.CurrentDirectory;

            while (!Directory.Exists(@_lovePath))
            {
                Console.Write("\nPath is wrong!, Agian: ");
                _lovePath = @Console.ReadLine();
            }
        }

        private void GetEntrance()
        {
            // I have no fucking clue how to read config without getting password - Useless Idea
        }
        #endregion

    }

}
