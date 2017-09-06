using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using RedXuCSharpClass;

namespace StalkerProject.OutputTerminal
{
    class ServerChan : STKWorker
    {
        private readonly HttpHelper _helper;
        public string SCKEY { get; set; }

        public const string RequestUrl = "https://sc.ftqq.com/{0}.send";
        private readonly ConcurrentQueue<string> _sendQueue;
        private AutoResetEvent _isQueue;
        private bool _stat;


        public ServerChan() : base()
        {
            _helper = new HttpHelper();
            _helper.SetEncoding(Encoding.UTF8);
            _sendQueue = new ConcurrentQueue<string>();
            _isQueue=new AutoResetEvent(false);
            _stat = true;
        }

        protected override void Run()
        {
            base.Run();
            string result = "";
            if (_sendQueue.TryDequeue(out result))
            {
                var ret = _helper.HttpPost(string.Format(RequestUrl, SCKEY), result);//_helper.HttpGet(result);
                var jsonDoc = JObject.Parse(ret);
                var err = jsonDoc["errno"].Value<int>();
                if (err != 0)
                {
                    var errtxt = jsonDoc["errmsg"].Value<string>();
                    Console.WriteLine("Serverchan send Error:" + errtxt);
                    if (errtxt == "bad pushtoken")
                    {
                        Console.WriteLine("Invalid Pushtoken,Exit ServerChan Module");
                        _stat = false;
                        return;
                    }
                }
            }
            else
            {
                //列表空，等待
                //_isQueue.WaitOne();
                WaitHandle.WaitAny(new WaitHandle[] {_isQueue, this.waitToken.WaitHandle});
            }
        }

        public override void LoadDefaultSetting()
        {
            int randInt = new Random().Next(1, 100000);
            Alias = "ServerChan" + randInt.ToString();
            Interval = 65000;
        }

        [STKDescription("录入新的数据")]
        public void InputData(string relatedAddress, string summary, string content, string relatedVar)
        {
            if (!_stat) return;
            var uriBuilder = new UriBuilder(string.Format(RequestUrl,SCKEY));
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["text"] = summary;
            parameters["desp"] = content + "\n来自:" + relatedAddress;
            //uriBuilder.Query = parameters.ToString();
            _sendQueue.Enqueue("text=" + summary + "&desp=" + content + "\n来自:" + relatedAddress);
            _isQueue.Set();
        }
    }
}
