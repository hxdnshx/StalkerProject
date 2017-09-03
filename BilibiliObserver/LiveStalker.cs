using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RedXuCSharpClass;

namespace StalkerProject.BilibiliObserver
{
    public class LiveStalker : STKWorker
    {
        public int TargetRoom { get; set; }
        private HttpHelper helper;
        private bool prevStatus = false;

        protected override void Prepare() {
            base.Prepare();
            helper = new HttpHelper();
            helper.SetUserAgent("Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36");
            helper.SetAccept("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            helper.SetEncoding(Encoding.UTF8);
        }

        protected override void Run() {
            base.Run();
            string ret = "";
            try
            {
                ret = helper.HttpGet("https://api.live.bilibili.com/live/getInfo?roomid=" + TargetRoom);
                JObject obj = JObject.Parse(ret);
                if (obj["code"].Value<int>() == -400)
                {
                    Console.WriteLine("无效的房间号ID:" + TargetRoom);
                    return;
                }
                bool status = obj["data"]["_status"].Value<string>() == "on";
                string title = obj["data"]["ROOMTITLE"].Value<string>();
                string nickname = obj["data"]["ANCHOR_NICK_NAME"].Value<string>();
                //Console.WriteLine(String.Format("Bilibili LiveRoom{0},Status:{1}", title, status ? "ON" : "OFF"));
                if (status != prevStatus)
                {
                    if (status == true)
                    {
                        DiffDetected?.Invoke(
                            "http://live.bilibili.com/" + TargetRoom,
                            nickname + "开启了自己的直播间！",
                            "直播间标题是：" + title,
                            "Bilibili.Live." + TargetRoom);
                    }
                    else
                    {
                        DiffDetected?.Invoke(
                            "http://live.bilibili.com/" + TargetRoom,
                            nickname + "关闭了自己的直播间！",
                            "直播间标题是：" + title,
                            "Bilibili.Live." + TargetRoom);
                    }
                    prevStatus = status;
                }
            }
            catch (Exception e)
            {
                File.AppendAllText("LiveError.log",
                    "\n" + e + "\n" + "收到包内容：\n" + ret);
            }
        }

        public override void LoadDefaultSetting()
        {
            base.LoadDefaultSetting();
            int randInt=new Random().Next(1,100000);
            Alias = "LiveStalker" + randInt;
            Interval = 300000;
        }

        public Action<string, string, string, string> DiffDetected { get; set; }
    }
}
