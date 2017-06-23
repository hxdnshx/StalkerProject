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
        private Thread workingThread;
        private bool pendingTerminate;
        public void Start()
        {
            if (workingThread == null)
            {
                workingThread = new Thread(Run);
                pendingTerminate = false;
                workingThread.Start();
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
                if (pendingTerminate) break;
                Process fetchProc=Process.Start(psi);
                fetchProc.WaitForExit();
                if(fetchProc.ExitCode!=0)
                    Console.WriteLine("Fetch Failed.");
                else
                {
                    File.WriteAllText(TargetUser + ".xml",JsonHelper.ConvertJsonToXml(TargetUser + ".json"));
                    Console.WriteLine("Message Fetched.");
                }

                Thread.Sleep(Interval<60000?60000:Interval);
            }
        }

        public void Stop()
        {
            pendingTerminate = true;
            workingThread.Join();
        }
    }
}
