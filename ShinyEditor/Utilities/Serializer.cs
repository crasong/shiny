using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ShinyEditor.Utilities
{
    public static class Serializer
    {
        public static void ToFileXML<T>(T instance, string path)
        {
            try
            {
                var serializer = new DataContractSerializer(typeof(T));
                var settings = new XmlWriterSettings() { Indent = true };
                using (var w = XmlWriter.Create(path, settings))
                {
                    serializer.WriteObject(w, instance);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // TODO: use a logger
            }
        }
        internal static T FromFileXML<T>(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open);
                var serializer = new DataContractSerializer(typeof(T));
                T instance = (T)serializer.ReadObject(fs);
                return instance;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // TODO: use a logger
                return default(T);
            }
        }
        public static void ToFileJSON<T>(T instance, string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Create);
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(fs, instance);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // TODO: use a logger
            }
        }
    }
}
