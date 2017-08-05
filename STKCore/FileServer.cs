using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject
{
    public class FileServer : DomainProxy
    {
        public static string WebDir { get; set; }
        public override void LoadDefaultSetting()
        {
            Alias = "FileServer" + new Random().Next(1, 10000);
            SubUrl = "";
            WebDir = "./www";
        }

        public static string CombineDir(string a, string b)
        {
            bool checka = a.EndsWith("/");
            bool checkb = b.StartsWith("/");
            if (checkb && checka)
                return a + b.Substring(1);
            else if (checkb || checka)
                return a + b;
            else
                return a + "/" + b;
        }

        public FileServer()
        {
            OnRequest += HandleRequest;
        }

        public override bool OnHttpRequest(HttpListenerContext context)
        {
            if (SubUrl==null) return false;
            if (context.Request.RawUrl.IndexOf(SubUrl) == 0) //In The Beginning
            {
                string reldir;
                if (SubUrl.Length > 0)
                    reldir = context.Request.RawUrl.Replace(SubUrl, "");
                else
                    reldir = context.Request.RawUrl;
                string dir = CombineDir(WebDir, reldir);
                if (!File.Exists(dir)) return false;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                var content = File.ReadAllBytes(dir);
                context.Response.OutputStream.Write(content,0,content.Length);
                context.Response.OutputStream.Close();
                context.Response.Close();
                return true;
            }
            return false;
        }

        public void HandleRequest(HttpListenerContext context, string subUrl)
        {
            
        }
    }
}
