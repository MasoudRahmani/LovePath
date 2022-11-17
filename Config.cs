﻿using LovePath.Util;
using System;
using System.Collections.Generic;
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
                Console.Write("(No Domain) Just User:");
                User = Console.ReadLine();
                while (!Utils.AccountExists(User))
                {
                    Console.Write("\nWrong User!, Again :");
                    User = Console.ReadLine();
                }

                Console.Write("\nLovePath:");
                _lovePath = @Console.ReadLine();
                while (!Directory.Exists(@_lovePath))
                {
                    Console.Write("\nPath is wrong!, Agian: ");
                    _lovePath = @Console.ReadLine();
                }

                var json = MySerialization.Serialize<Config>(this);

                bool done = false;
                do
                {
                    Console.Write($"<{DomainName}\\{User}> config, Enter ");
                    var config_pass = Utils.GetInputPassword();

                    done = RunImpersonated(DomainName, User, config_pass, () =>
                    {
                        Utils.WriteFileSecurely(configFilePath, json, User, true);
                    });
                    if (done)
                    {
                        Password = config_pass;
                        UseInitialPassword = true;
                    }
                    else Console.WriteLine("Something went wrong! try again.");

                } while (!done);
            }
            else
            {
                var rules = Utils.GetFileAccessRule(configFilePath);
                var wellknownacc = Utils.GetWellKnownSidsName();
                List<string> validUsers = new List<string>();

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
                    var config_pass = Utils.GetInputPassword();

                    done = RunImpersonated(config_domain, config_user, config_pass, () =>
                    {
                        var cnf = MySerialization.Deserialize<Config>(new StreamReader(configFilePath).ReadToEnd());

                        if (cnf.User == config_user && cnf.DomainName == config_domain)
                            UseInitialPassword = true; //If config permission had the same user as user in config file we have the password, otherwise we need to ask again.
                        Password = config_pass;

                        ProgramPath = cnf.ProgramPath;
                        ConfigFileName = cnf.ConfigFileName;
                        DomainName = cnf.DomainName;

                        User = cnf.User;
                        XplorerName = cnf.XplorerName;
                        LovePath = cnf.LovePath;
                    });

                    if (!done) Console.WriteLine("Something went wrong! try again.");
                } while (!done);
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

        private bool RunImpersonated(string domain, string user, string pass, Action action)
        {
            using (var imp = new UserImpersonation2())
            {
                if (imp.ImpersonateValidUser(user, domain, pass))
                    action.Invoke();
                else
                    return false;
            }
            return true;
        }
    }

}