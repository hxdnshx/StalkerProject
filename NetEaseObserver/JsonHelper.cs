using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace StalkerProject.NetEaseObserver
{
    class JsonHelper
    {
        public static void ConvertJson(JToken element, XElement doc, string eleName, string prefixData = "")
        {
            XElement subroot;
            doc.Add(subroot = new XElement(
                eleName,
                new XAttribute(
                    "Prefix",
                    prefixData)));
            if (element is JValue)
            {
                subroot.SetValue(((JValue)element).Type != JTokenType.Null ? element.Value<string>() : "");
            }
            else if (element is JObject)
            {
                foreach (var innerElement in (JObject)element)
                {
                    ConvertJson(innerElement.Value, subroot, innerElement.Key, prefixData + "/" + eleName);
                }
            }
            else if (element is JArray)
            {
                foreach (var innerElement in ((JArray)element))
                {
                    ConvertJson(innerElement, subroot, "Element", prefixData + "/" + eleName);
                }
            }
        }

        public static string ConvertJsonToXml(string path)
        {
            XDocument doc = new XDocument();
            JObject jsonDoc = JObject.Parse(File.ReadAllText(path));
            var root = new XElement("NeteaseData");
            doc.Add(root);
            ConvertJson(jsonDoc, root, "root", ".");
            return doc.ToString();
        }
    }
}
