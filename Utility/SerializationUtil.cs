using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace LovePath.Utility
{
    public static class SerializationUtil
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
}
