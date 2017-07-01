using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using StalkerProject;
using StalkerProject.NianObserver;

namespace StalkerProject
{
    public class DomainProxy : ISTKService
    {
        public string Alias { get; set; }
        public string SubUrl { get; set; }
        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }

        public void LoadDefaultSetting()
        {
            Alias = "DomainProxy" + new Random().Next(1, 10000);
            SubUrl = "";
        }

        public bool OnHttpRequest(HttpListenerContext request)
        {
            if (string.IsNullOrWhiteSpace(SubUrl)) return false;
            if (request.Request.RawUrl.IndexOf(SubUrl) == 0) //In The Beginning
            {
                if (OnDataFetched == null) return false;
                if (OnDataFetched.GetInvocationList().Length > 1)
                    throw new ArgumentException("OnDataFetched不能接受多个连接");
                OnDataFetched(request);
                return true;
            }
            return false;
        }

        [STKDescription("收到网页请求时")]
        public Action<HttpListenerContext> OnDataFetched { get; set; }
    }
    class Program
    {
        public static void AddAddress(string address, string domain, string user)
        {
            string argsDll = String.Format(@"http delete urlacl url={0}", address);
            string args = string.Format(@"http add urlacl url={0} user={1}\{2}", address, domain, user);
            ProcessStartInfo psi = new ProcessStartInfo("netsh", argsDll);
            psi.Verb = "runAs";
            //psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;
            Process.Start(psi).WaitForExit();//删除urlacl
            psi = new ProcessStartInfo("netsh", args);
            psi.Verb = "RunAs";
            //psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;
            Process.Start(psi).WaitForExit();//添加urlacl
        }

        private static string ShellPath = "shell.txt";
        static void Main(string[] args)
        {
            NianObserver.NianApi inst=new NianApi();
            bool loginFlag = false;
            if (File.Exists(ShellPath))
            {
                string[] data = File.ReadAllLines(ShellPath);
                if (inst.RestoreLogin(data[0], data[1]))
                    loginFlag = true;
            }
            if (loginFlag==false && inst.Login("741782800@qq.com", "17672155"))//Short Circuit
            {
                string uid="";
                string shell="";
                inst.GetLoginToken(out uid,out shell);
                File.WriteAllText(ShellPath,uid + "\r\n" + shell);
            }
            ServiceManager manager = new ServiceManager();
            manager.ReadSetting("serviceSetting.xml");
            manager.SaveSetting("serviceSetting.xml");
            foreach (var srv in manager.ActiveServices)
            {
                srv.Start();
            }
            AddAddress("http://*:8081/", System.Environment.MachineName, System.Environment.UserName);
            using (HttpListener server = new HttpListener())
            {
                server.Prefixes.Add(@"http://*:8081/");
                server.Start();
                for (;;)
                {
                    var result = server.GetContext();
                    bool isHandled = false;
                    Console.WriteLine(result);
                    foreach (var srv in manager.ActiveServices)
                    {
                        if (srv is DomainProxy)
                        {
                            var proxy = srv as DomainProxy;
                            if (proxy.OnHttpRequest(result))
                            {
                                isHandled = true;
                                break;
                            }
                        }
                    }
                    if (!isHandled && result.Request.RawUrl.Equals("/quit"))
                    {
                        using (StreamWriter writer = new StreamWriter(result.Response.OutputStream))
                        {
                            writer.Write("Service Terminated.");
                            writer.Close();
                            result.Response.Close();
                        }
                        break;
                    }

                }
                server.Stop();
                foreach (var srv in manager.ActiveServices)
                {
                    srv.Stop();
                }
            }
        }
    }
}
