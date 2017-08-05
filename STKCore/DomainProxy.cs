using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

        public virtual void LoadDefaultSetting()
        {
            Alias = "DomainProxy" + new Random().Next(1, 10000);
            SubUrl = "";
        }

        public virtual bool OnHttpRequest(HttpListenerContext request)
        {
            if (string.IsNullOrWhiteSpace(SubUrl)) return false;
            if (request.Request.RawUrl.IndexOf(SubUrl) == 0) //In The Beginning
            {
                if (OnRequest == null) return false;
                if (OnRequest.GetInvocationList().Length > 1)
                    throw new ArgumentException("OnDataFetched不能接受多个连接");
                OnRequest(request, SubUrl);
                return true;
            }
            return false;
        }

        [STKDescription("收到网页请求时")]
        public Action<HttpListenerContext, string> OnRequest { get; set; }
    }
}
