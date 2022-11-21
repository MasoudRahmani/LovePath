using LovePath.Util;
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
        private string _domainName = Environment.UserDomainName;
        private string _user = Environment.UserName;
        private string _xplorerName = "XY.exe";
        private string _configFileName = "LoveConfig.json";
        private string _lovePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private List<ConsoleKey> _secret;
        #endregion

        #region Property

        [DataMember] //If Config file was moved, this is here to check config file and find last path
        public string ProgramPath { get { return _programPath; } private set { _programPath = value; } }
        [DataMember]
        public string DomainName { get { return _domainName; } private set { _domainName = value; } }
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
        public string FullAccountName { get { return $"{_domainName}\\{_user}"; } private set {; } } // from User
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
                Utils.HelpInConsole(key);
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
                CreateConfig();
            else
                ReadConfig();

            if (!File.Exists(ExplorerFullPath))
                throw new Exception(
                    "Explorer Not found!\nMake sure Third-party Explorer is located beside main program.\n" +
                    "If you have changed the name, change the name in config too.");
        }

        /// <summary>
        /// Ask user for password and set/change fields for later use
        /// </summary>
        public void GetPassword()
        {
            var password = ConsoleUtils.GetInputPassword();
            SecurePassword = Utils.ConvertToSecureString(password);
            SecurePassword.MakeReadOnly();
        }

        #region Private Methods

        private void CreateConfig()
        {
            GetAccountName();
            GetLovePath();
            GetExplorerName();

            var json = MySerialization.Serialize(this);

            bool done = false;
            do
            {
                Console.Write($"\"{FullAccountName}\" config, Enter ");
                GetPassword();

                var impersonation = new ImpersonateUser(ImpersonationType.UserImpersonation2, DomainName, User, SecurePassword);
                done = impersonation.RunImpersonated(() =>
                {
                    Utils.WriteFile(ConfigFullPath, json);
                    SysSecurityUtils.ClearFileAccessRule(ConfigFullPath);

                    SysSecurityUtils.AllowFileAccessRule(ConfigFullPath, FullAccountName, FileSystemRights.FullControl);
                    SysSecurityUtils.AllowFileAccessRule(ConfigFullPath, WellKnownSidType.BuiltinUsersSid, FileSystemRights.ReadPermissions);

                    //File.Encrypt(ConfigFullPath); //Encrypt after setting access control seems to solve firs time run issue

                });
                if (done)
                    UseInitialPassword = true;
                else
                {
                    UseInitialPassword = false;
                    Console.WriteLine("Something went wrong! try again.");
                }

            } while (!done);
        }

        private void ReadConfig()
        {
            var rules = SysSecurityUtils.GetFileAccessRule(ConfigFullPath);
            var wellknownacc = SysSecurityUtils.GetWellKnownSidsName();
            var validUsers = new List<string>();

            foreach (FileSystemAccessRule rule in rules)
            {
                var found = wellknownacc.Find(x => x.ToLowerInvariant().Contains(rule.IdentityReference.Value.ToLowerInvariant()));
                if (string.IsNullOrWhiteSpace(found))
                {
                    if (FileSystemRights.FullControl == rule.FileSystemRights)
                        validUsers.Add(rule.IdentityReference.Value);
                }
            }
            var cFullAccount = validUsers[0].Split('\\');
            var cDomain = cFullAccount[0];
            var cUser = cFullAccount[1];

            bool done = false;
            do
            {
                Console.Write($"\"{validUsers[0]}\" config, Enter ");
                GetPassword();

                var impersonate = new ImpersonateUser(ImpersonationType.UserImpersonation2, cDomain, cUser, SecurePassword);

                done = impersonate.RunImpersonated(() =>
                {
                    var json = File.ReadAllText(ConfigFullPath);
                    var cnf = MySerialization.Deserialize<Config>(json);

                    if (cnf.User == cUser && cnf.DomainName == cDomain)
                        UseInitialPassword = true; //If config permission had the same user as user in config file we have the password, otherwise we need to ask again.

                    DomainName = cnf.DomainName;

                    User = cnf.User;
                    XplorerName = cnf.XplorerName;
                    LovePath = cnf.LovePath;
                });
                if (!done) Console.WriteLine("Something went wrong! try again.");
            } while (!done);
        }

        private void GetExplorerName()
        {
            Console.Write(
                "\n\tPut <Explorer> in Application Directory" +
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

            while (!Directory.Exists(@_lovePath))
            {
                Console.Write("\nPath is wrong!, Agian: ");
                _lovePath = @Console.ReadLine();
            }
        }

        private void GetAccountName()
        {
            Console.Write("(No Domain) Just User:");
            User = Console.ReadLine();

            while (!SysSecurityUtils.AccountExists(User))
            {
                Console.Write("\nWrong User!, Again :");
                User = Console.ReadLine();
            }
        }

        private void GetEntrance()
        {
            // I have no fucking clue how to read config without getting password - Useless Idea
        }
        #endregion

    }

}