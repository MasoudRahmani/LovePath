using LovePath.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;

namespace LovePath
{
    [DataContract]
    public class Config
    {
        #region Fields and Property
        [DataMember]
        public string ProgramPath = AppDomain.CurrentDomain.BaseDirectory;
        [DataMember]
        public string ConfigFileName = "LovePathConfig.json";
        [DataMember]
        public string DomainName = Environment.UserDomainName;
        [DataMember]
        public string FullAccountName = $"{Environment.UserName}\\{Environment.UserName}";
        //--------------------------------------------------

        private string _user = Environment.UserName;
        private string _xplorerName = "Explorer++.exe";
        private string _lovePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [DataMember]
        public string User
        {
            get => _user;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new NullReferenceException();
                else _user = value;
            }
        }
        [DataMember]
        public string XplorerName
        {
            get => _xplorerName;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new NullReferenceException();
                else _xplorerName = value;
            }
        }
        [DataMember]
        public string LovePath
        {
            get => _lovePath;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new NullReferenceException();
                else _lovePath = value;
            }

        }
        //-------------------------------------------------

        public string ExplorerFullPath; // from init()
        public string Password;
        public bool UseInitialPassword = false; //If config permission had the same user as user in config file we have the password, otherwise we need to ask again.
        public string ConfigFullPath;
        //-----------------------------------------------

        #endregion

        /// <summary>
        /// If there is no Config file located at Hardoced Destination, will create it otherwise read and initialise.
        /// </summary>
        public void Init()
        {
            ConfigFullPath = Path.Combine(ProgramPath, ConfigFileName);

            if (!File.Exists(ConfigFullPath))
                CreateConfig();
            else
                GetConfigSettings();

            if (!File.Exists(ExplorerFullPath))
                throw new Exception(
                    "Explorer Not found!\nMake sure Third-party Explorer is located beside main program.\n" +
                    "If you have changed the name, change the name in config too.");
        }

        public void ChangePassword()
        {
            Password = ConsoleUtils.GetInputPassword();
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
                Console.Write($"{FullAccountName} config, Enter ");
                var cnfPassword = ConsoleUtils.GetInputPassword();

                var impersonation = new ImpersonateUser(ImpersonationType.UserImpersonation2, DomainName, User, cnfPassword);
                done = impersonation.RunImpersonated(() =>
                {
                    Utils.WriteEncryptedFile(ConfigFullPath, json, false);//encryption has bug. can't read file after restart

                    SysSecurityUtils.ClearFileAccessRule(ConfigFullPath);

                    SysSecurityUtils.AllowFileAccessRule(ConfigFullPath, FullAccountName, FileSystemRights.FullControl);
                    SysSecurityUtils.AllowFileAccessRule(ConfigFullPath, WellKnownSidType.BuiltinUsersSid, FileSystemRights.ReadPermissions);
                });
                if (done)
                {
                    Password = cnfPassword;
                    UseInitialPassword = true;
                }
                else Console.WriteLine("Something went wrong! try again.");

            } while (!done);
        }

        private void GetExplorerName()
        {
            Console.Write(
                "\n----- Place Explorer beside program ------" +
                "\nExplorer Name:");
            _xplorerName = @Console.ReadLine();


            ExplorerFullPath = Path.Combine(ProgramPath, _xplorerName);
            if (!File.Exists(ExplorerFullPath))
            {
                Console.Write("\nWrong name!, Agian: ");
                _xplorerName = @Console.ReadLine();
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
            FullAccountName = $"{DomainName}\\{User}";
        }

        private void GetConfigSettings()
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
            var config_permission = validUsers[0].Split('\\');
            var config_domain = config_permission[0];
            var config_user = config_permission[1];

            bool done = false;
            do
            {
                Console.Write($"<{string.Join("\\", config_permission)}> config, Enter ");
                var config_pass = ConsoleUtils.GetInputPassword();

                var impersonate = new ImpersonateUser(ImpersonationType.UserImpersonation2, config_domain, config_user, config_pass);
                done = impersonate.RunImpersonated(() =>
                {
                    var cnf = MySerialization.Deserialize<Config>(new StreamReader(ConfigFullPath).ReadToEnd());

                    if (cnf.User == config_user && cnf.DomainName == config_domain)
                        UseInitialPassword = true; //If config permission had the same user as user in config file we have the password, otherwise we need to ask again.
                    Password = config_pass;

                    ProgramPath = cnf.ProgramPath; //?? why
                    ConfigFileName = cnf.ConfigFileName; //?? Why
                    DomainName = cnf.DomainName;

                    User = cnf.User;
                    XplorerName = cnf.XplorerName;
                    LovePath = cnf.LovePath;

                    FullAccountName = $"{DomainName}\\{User}";
                    ExplorerFullPath = Path.Combine(ProgramPath, XplorerName);
                });
                if (!done) Console.WriteLine("Something went wrong! try again.");
            } while (!done);
        }

        #endregion


    }

}