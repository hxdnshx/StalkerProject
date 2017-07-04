using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace StalkerProject.NetEaseObserver
{
    class NetEaseAnalyse : ISTKService
    {
        public string Alias { get; set; }
        public string WorkingDir { get; set; }

        public void Start()
        {
            FileHelper.ResolvePath(WorkingDir);
            //Nothing to do...
        }

        public void Stop()
        {
            //Nothing to do
        }

        public void LoadDefaultSetting()
        {
            Alias = "NetEaseAnalyse" + new Random().Next(1, 10000);
            WorkingDir = "./AnalyseHistory";
        }

        

        [STKInputPort]
        public void OnDataUpdated(JObject data)
        {
            
        }
    }


}
