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

        public static void WriteEncryptedFile(string path, string data, bool encrypted = false)
        {
            var encrypt = (encrypted) ? FileOptions.Encrypted : FileOptions.WriteThrough;
            var fs = File.Create(path, 4096, encrypt);

            using (fs)
            {
                var byt = Encoding.UTF8.GetBytes(data);
                fs.Write(byt, 0, byt.Length);
            }
        }

        public static void WriteTempFile(string path, string data = "No Data.")
        {
            var fs = File.Create(path, 4096, FileOptions.Encrypted);
            using (fs)
            {
                var byt = Encoding.UTF8.GetBytes(data);
                fs.Write(byt, 0, byt.Length);
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
