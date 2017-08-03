using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
}
