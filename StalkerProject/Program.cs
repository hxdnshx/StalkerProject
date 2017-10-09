using System;
using System.Diagnostics;
using System.IO;
using System.Net;


/*
        ~~献给自己已经逝去的，名为“初恋”的感情。~~妈的太蠢了你就这么喜欢STK喽？（虽然感觉对自己已经没用了吧
     */
namespace StalkerProject
{

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
        
        static void Main(string[] args)
        {
            ServiceManager manager = new ServiceManager();
            manager.ReadSetting("serviceSetting.xml");
            //manager.SaveSetting("serviceSetting.xml");
            
            //AddAddress("https://*:8082/", System.Environment.MachineName, System.Environment.UserName);
            using (HttpListener server = new HttpListener())
            {
                server.IgnoreWriteExceptions = true;
                server.Prefixes.Add(@"http://*:8081/");
                try
                {
                    server.Start();
                }
                catch (Exception e)
                {
                    //这里一般产生的是这个端口已经被占用，之类的错误
                    //可以用这里的异常来检测多实例，防止可能的数据库被损坏
                    Console.WriteLine(e);
                    return;
                }
                //服务的启动放在后面，防止多个实例碰撞
                foreach (var srv in manager.ActiveServices)
                {
                    srv.Start();
                }
                for (;;)
                {
                    var result = server.GetContext();
                    bool isHandled = false;
                    //Console.WriteLine(result);
                    foreach (var srv in manager.ActiveServices)
                    {
                        if (srv is IDomainProxy)
                        {
                            var proxy = srv as IDomainProxy;
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
                    if (!isHandled) {
                        result.Response.StatusCode = 404;
                        using (StreamWriter writer = new StreamWriter(result.Response.OutputStream))
                        {
                            writer.Write("404.");
                            writer.Close();
                            result.Response.Close();
                        }
                        continue;
                    }
                }
                foreach (var srv in manager.ActiveServices)
                {
                    Console.WriteLine($"Waiting For {srv.Alias}...");
                    srv.Stop();
                }
                manager.ActiveServices.Clear();
                GC.Collect();
                server.Stop();
                Console.WriteLine("StalkerProject Terminated.\n\n\n\n\n\n\n");
            }
        }
    }
}
