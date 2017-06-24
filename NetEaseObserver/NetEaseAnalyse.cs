using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject.NetEaseObserver
{
    class NetEaseAnalyse : ISTKService
    {
        public string Alias { get; set; }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void LoadDefaultSetting()
        {
            throw new NotImplementedException();
        }

        [STKInputPort]
        public void OnDataUpdated(string data)
        {
            
        }
    }
}
