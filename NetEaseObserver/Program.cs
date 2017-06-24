using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Xml.Linq;

namespace StalkerProject.NetEaseObserver
{
    class NetEaseObserver
    {

        

        public static void AddAddress(string address, string domain, string user)
        {
            string argsDll = String.Format(@"http delete urlacl url={0}", address);
            string args = string.Format(@"http add urlacl url={0} user={1}\{2}", address, domain, user);
            ProcessStartInfo psi = new ProcessStartInfo("netsh", argsDll);
            psi.Verb = "runAs";
            //psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process.Start(psi).WaitForExit();//删除urlacl
            psi = new ProcessStartInfo("netsh", args);
            psi.Verb = "RunAs";
            //psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process.Start(psi).WaitForExit();//添加urlacl
        }

        static void Main(string[] args)
        {
            ServiceManager manager=new ServiceManager();
            manager.ReadSetting("serviceSetting.xml");
            AddAddress("http://*:8081/",System.Environment.MachineName,System.Environment.UserName);
            using (HttpListener server = new HttpListener())
            {
                NetEaseFetch fetch=new NetEaseFetch();
                fetch.TargetUser = "UshioC";
                fetch.Start();
                server.Prefixes.Add(@"http://*:8081/");
                server.Start();
                for (;;)
                {
                    var result = server.GetContext();
                    Console.WriteLine(result);
                    if (result.Request.RawUrl.Equals("/quit"))
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
            }
        }
    }
}
