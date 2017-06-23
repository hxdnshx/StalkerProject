using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject.NetEaseObserver
{
    interface ISTKService
    {
        /// <summary>
        /// 用于标识这个服务的别名
        /// </summary>
        string Alias { get; set; }
        void Start();
        void Stop();

    }
}
