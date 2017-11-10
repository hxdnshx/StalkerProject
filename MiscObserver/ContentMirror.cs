using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject.MiscObserver
{
    class ContentMirror : FileServer
    {
        public string MirrorDir { get; set; }
        private HttpClient client;

        public override void LoadDefaultSetting()
        {
            base.LoadDefaultSetting();
            Alias = "ContentMirror" + new Random().Next(1, 10000);
            MirrorDir = "http://www.baidu.com/";
        }

        public ContentMirror()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A356 Safari/604.1");
            //client.DefaultRequestHeaders.Add("Content-Type", "application /x-www-form-urlencoded");
            client.DefaultRequestHeaders.Add("Referer", "http://nian.so/m/step/");
            //client.DefaultRequestHeaders.Add("Origin", "http://music.163.com");
            client.DefaultRequestHeaders.Add("Host", "img.nian.so");
            client.DefaultRequestHeaders.Add("Accept", "image/webp,image/*,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.8");
        }

        public override bool OnHttpRequest(HttpListenerContext context)
        {
            if (base.OnHttpRequest(context)) return true;
            if (SubUrl == null) return false;
            string rawurl = context.Request.RawUrl;
            if (rawurl.Length > 1 && (rawurl[0] == '/' || rawurl[0] == '\\'))
                rawurl = rawurl.Substring(1);
            if (SubUrl.Length > 1 && (SubUrl[0] == '/' || SubUrl[0] == '\\'))
                SubUrl = SubUrl.Substring(1);
            if (rawurl.IndexOf(SubUrl) == 0) //In The Beginning
            {
                string reldir;
                if (SubUrl.Length > 0)
                    reldir = rawurl.Replace(SubUrl, "");
                else
                    reldir = rawurl;
                string dir = CombineDir(MirrorDir, reldir);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Async Response
                Task t = Task.Run(() => {
                    bool isSuccess = true;
                    try
                    {
                        var result = client.GetAsync(dir).Result;
                        if (!result.IsSuccessStatusCode)
                        {
                            context.Response.StatusCode = 404;
                            context.Response.Close();
                        }
                        string saveDir = CombineDir(WebDir, reldir);
                        FileHelper.ResolvePath(saveDir);
                        FileStream stream = new FileStream(saveDir, FileMode.Create);
                        var src = result.Content.ReadAsStreamAsync().Result;
                        src.CopyTo(stream);
                        src.Seek(0, SeekOrigin.Begin);
                        src.CopyTo(context.Response.OutputStream);
                        src.Close();
                        context.Response.OutputStream.Close();
                        stream.Flush();
                        stream.Close();
                        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        context.Response.Close();
                    }
                    catch(Exception e)
                    {
                        isSuccess = false;
                        Console.WriteLine(e);
                    }
                    
                    sw.Stop();
                    Console.WriteLine("Request:" + dir + (isSuccess?"Handled":"Failed") + " in " + sw.ElapsedMilliseconds + "ms");
                });
                return true;
            }
            return false;
        }

        public static byte[] Decompress_GZip(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                                       CompressionMode.Decompress))
            {
                byte[] buffer = new byte[1024];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, 1024);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
