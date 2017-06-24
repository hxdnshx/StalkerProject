using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StalkerProject.NetEaseObserver
{
    class NetEaseFetch : ISTKService
    {
        public int Interval { get; set; }
        public string TargetUser { get; set; }
        private Thread _workingThread;
        private bool _pendingTerminate;
        public string Alias { get; set; }

        public void Start()
        {
            if (_workingThread == null)
            {
                _workingThread = new Thread(Run);
                _pendingTerminate = false;
                _workingThread.Start();
            }
        }

        private void Run()
        {
            ProcessStartInfo psi = new ProcessStartInfo("casperjs", "netease.js \"" + TargetUser + "\" \"" + TargetUser + ".json\"");
            //psi.RedirectStandardOutput = true;
            psi.Verb = "RunAs";
            psi.UseShellExecute = false;
            for (;;)
            {
                if (_pendingTerminate) break;
                Process fetchProc=Process.Start(psi);
                fetchProc.WaitForExit();
                if(fetchProc.ExitCode!=0)
                    Console.WriteLine("Fetch Failed.");
                else
                {
                    string ret = JsonHelper.ConvertJsonToXml(TargetUser + ".json");
                    File.WriteAllText(TargetUser + ".xml",ret);
                    OnDataFetched?.Invoke(ret);
                    Console.WriteLine("Message Fetched.");
                }

                Thread.Sleep(Interval<60000?60000:Interval);
            }
        }

        public void Stop()
        {
            _pendingTerminate = true;
            _workingThread.Join();
        }

        public void LoadDefaultSetting()
        {
            Interval = 60000;
            TargetUser = "InTheFlickering";
            Alias = "NetEaseFetch" + new Random().Next(1, 10000);
        }

        [STKDescription("当新的数据被拉取时")]
        public Action<string> OnDataFetched { get; set; }
    }
}
