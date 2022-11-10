using System;
using System.Runtime.Serialization;

namespace LovePath
{
    [DataContract]
    public class Config
    {
        private string _user = "Hidden";
        private string _xplorerName = "XY.exe";
        private string _lovePath = @"E:\";

        [DataMember]
        public string ProgramPath = AppDomain.CurrentDomain.BaseDirectory;
        [DataMember]
        public string ConfigFileName = "lovepathconfig.json";
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
    }

}
