using LovePath.Util;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.AccessControl;

namespace LovePath
{
    [DataContract]
    public class Config
    {
        [DataMember]
        public string ProgramPath = AppDomain.CurrentDomain.BaseDirectory;
        [DataMember]
        public string ConfigFileName = "lovepathconfig.json";
        [DataMember]
        public string DomainName = Environment.UserDomainName;
        //--------------------------------------------------

        //-----------------------------------------------
        private string _user = "Hidden";
        private string _xplorerName = "Explorer++.exe";
        private string _lovePath = @"E:\";

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

        public Config()
        {
        }

        /// <summary>
        /// If there is no Config file located at Hardoced Destination, will create it otherwise read and initialise.
        /// </summary>
        public void Init()
        {
            string configFilePath = Path.Combine(ProgramPath, ConfigFileName);

            if (!File.Exists(configFilePath))
            {
                Console.Write("No Domain (currently local) User :");
                User = Console.ReadLine();

                while (!Utils.AccountExists(User))
                {
                    Console.Write("Wrong User!, Again :");
                    User = Console.ReadLine();
                }

                Console.Write("\nLovePath:");
                _lovePath = @Console.ReadLine();
                while (!Directory.Exists(@_lovePath))
                {
                    Console.WriteLine("Path is wrong!, Agian: ");
                    _lovePath = @Console.ReadLine();
                }
                var json = MySerialization.Serialize<Config>(this);
                Utils.WriteFileSecurely(configFilePath, json, User, false);
            }
            else
            {
                FileInfo fi = new FileInfo(configFilePath);
                FileSecurity fs = fi.GetAccessControl();

                AuthorizationRuleCollection rules = fs.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

                if (rules.Count > 1) throw new Exception("Config file security permission is modified, delete it or change it back");

                string[] config_permission = rules[0].IdentityReference.Value.Split('\\');
                var config_domain = config_permission[0];
                var config_user = config_permission[1];

                Console.Write($"config of: <{string.Join("\\", config_permission)}>, ");
                var config_pass = Utils.GetInputPassword();

                Config cnf;
                using (var imp = new UserImpersonation2())
                {
                    bool sucess = false;
                    while (!sucess)
                    {
                        sucess = imp.ImpersonateValidUser(config_user, config_domain, config_pass);

                        if (sucess)
                        {
                            cnf = MySerialization.Deserialize<Config>(new StreamReader(configFilePath).ReadToEnd());
                            if (cnf.User == config_user && cnf.DomainName == config_domain)
                                UseInitialPassword = true; //If config permission had the same user as user in config file we have the password, otherwise we need to ask again.

                            Password = config_pass;

                            ProgramPath = cnf.ProgramPath;
                            ConfigFileName = cnf.ConfigFileName;
                            DomainName = cnf.DomainName;

                            User = cnf.User;
                            XplorerName = cnf.XplorerName;
                            LovePath = cnf.LovePath;
                        }
                        else
                        {
                            Console.Write($"config of: <{string.Join("\\", config_permission)}>, ");
                            config_pass = Utils.GetInputPassword();
                        }
                    }
                }
            }

            ExplorerFullPath = Path.Combine(ProgramPath, XplorerName);

            if (!File.Exists(ExplorerFullPath))
            {
                throw new Exception("Explorer Not found!\nMake sure Third-party Explorer is located beside main program.\nIf you have changed the name, change the name in config too.");
            }
            Console.Clear();
        }

        public void ChangePassword()
        {
            Password = Utils.GetInputPassword();
            Console.Clear();
        }


    }

}
