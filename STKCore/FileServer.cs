using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StalkerProject
{
    public class FileServer : DomainProxy
    {
        public string WebDir { get; set; }
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
            string rawurl = context.Request.RawUrl;
            if (rawurl.Length > 1 && (rawurl[0] == '/' || rawurl[1] == '\\'))
                rawurl = rawurl.Substring(1);
            if (SubUrl.Length > 1 && (SubUrl[0] == '/' || SubUrl[1] == '\\'))
                SubUrl = SubUrl.Substring(1);
            if (rawurl.IndexOf(SubUrl) == 0) //In The Beginning
            {
                string reldir;
                if (SubUrl.Length > 0)
                    reldir = rawurl.Replace(SubUrl, "");
                else
                    reldir = rawurl;
                string dir = CombineDir(WebDir, reldir);
                if (!File.Exists(dir)) return false;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Async Response
                Task t = Task.Run(() => {
                    
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    var content = File.ReadAllBytes(dir);
                    context.Response.OutputStream.Write(content, 0, content.Length);
                    context.Response.OutputStream.Close();
                    context.Response.Close();
                    sw.Stop();
                    Console.WriteLine("Request:" + dir + "Handled in " + sw.ElapsedMilliseconds + "ms");
                });
                return true;
            }
            return false;
        }

        public void HandleRequest(HttpListenerContext context, string subUrl)
        {
            
        }
    }
}
