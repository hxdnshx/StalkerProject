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
    public class NetEaseFetch : STKWorker
    {
        public string TargetUser { get; set; }

        protected override void Run()
        {
            ProcessStartInfo psi = new ProcessStartInfo("casperjs",
                "netease.js \"" + TargetUser + "\" \"" + TargetUser + ".json\"") {
                RedirectStandardOutput = false,
                UseShellExecute = false
            };
            //psi.RedirectStandardOutput = true;
            //psi.Verb = "RunAs";
            if (IsFirstRun && !IsTest) {
                var randomOffset = new Random().Next(0, 1000 * 600);
                waitToken.WaitHandle.WaitOne(randomOffset);
            }
            Process fetchProc = Process.Start(psi);
            fetchProc.WaitForExit();
            if (fetchProc.ExitCode != 0)
                Console.WriteLine("Fetch Failed.");
            else
            {
                string ret = File.ReadAllText(TargetUser + ".json");
                JObject obj = JObject.Parse(ret);
                OnDataFetched?.Invoke(obj);
                //Console.WriteLine("Message Fetched.");
            }
        }

        public override void LoadDefaultSetting()
        {
            base.LoadDefaultSetting();
            Interval = 60000;
            TargetUser = "InTheFlickering";
            Alias = "NetEaseFetch" + new Random().Next(1, 10000);
        }

        [STKDescription("当新的数据被拉取时")]
        public Action<JObject> OnDataFetched { get; set; }
    }
}
