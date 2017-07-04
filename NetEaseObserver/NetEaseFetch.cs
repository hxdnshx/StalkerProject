using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThreadState = System.Threading.ThreadState;

namespace StalkerProject.NetEaseObserver
{
    class NetEaseFetch : ISTKService
    {
        public int Interval { get; set; }
        public string TargetUser { get; set; }
        private Thread workingThread;
        private AutoResetEvent isTerminate;
        public string Alias { get; set; }

        public void Start()
        {
            if (workingThread == null)
            {
                workingThread = new Thread(Run);
                isTerminate=new AutoResetEvent(false);
                workingThread.Start();
            }
        }

        private void Run()
        {
            ProcessStartInfo psi = new ProcessStartInfo("casperjs", "netease.js \"" + TargetUser + "\" \"" + TargetUser + ".json\"");
            //psi.RedirectStandardOutput = true;
            //psi.Verb = "RunAs";
            psi.UseShellExecute = false;
            for (;;)
            {
                
                Process fetchProc=Process.Start(psi);
                fetchProc.WaitForExit();
                if(fetchProc.ExitCode!=0)
                    Console.WriteLine("Fetch Failed.");
                else
                {
                    string ret = File.ReadAllText(TargetUser + ".json");
                    JObject obj=JObject.Parse(ret);
                    OnDataFetched?.Invoke(TargetUser,obj);
                    Console.WriteLine("Message Fetched.");
                }

                if (isTerminate.WaitOne(Math.Max(Interval, 60000)))
                    break;
            }
        }

        public void Stop()
        {
            isTerminate.Set();
            workingThread.Join();
        }

        public void LoadDefaultSetting()
        {
            Interval = 60000;
            TargetUser = "InTheFlickering";
            Alias = "NetEaseFetch" + new Random().Next(1, 10000);
        }

        [STKDescription("当新的数据被拉取时")]
        public Action<string,JObject> OnDataFetched { get; set; }
    }
}
