using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StalkerProject
{
    public static class NetworkHelper
    {
        public static void ResponseString(this HttpListenerContext context, string str)
        {
            context.Response.ContentEncoding=Encoding.UTF8;
            context.Response.Headers.Add("Access-Control-Allow-Origin","*");
            using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(str);
                writer.Close();
                context.Response.Close();
            }
        }
    }

    public class IntervalGate
    {
        private System.Threading.Timer _timer;
        private int _interval;
        private Semaphore _semaphore;
        public IntervalGate(int milliInterval)
        {
            _semaphore = new Semaphore(1,1);
            _timer = new Timer(_timeout,null,-1,milliInterval);
        }

        private void _timeout(object data)
        {
            _semaphore.Release();
        }
    }
}
