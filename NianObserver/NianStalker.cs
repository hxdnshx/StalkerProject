using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json.Linq;

namespace StalkerProject.NianObserver
{
    public class NianStalker : ISTKService
    {
        public int TargetUID { get; set; }
        public int Interval { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string Alias { get; set; }
        private string session => Alias + ".session";
        private string database => Alias + ".db";
        private NianApi api;
        private Task updateJob;
        private CancellationTokenSource isCancel;
        private LiteDatabase db;
        private NianData data;
        private string uName;
        private DateTime currentTime;
        /// <summary>
        /// 对应数据项的增量抓取函数表
        /// </summary>
        private Dictionary<string, Action<string,NianData>> IncrementalFetch;

        private void GetDreamExtendInfo(DreamInfo dream, NianData data)
        {
            int maxPage = 1;
            for (int page = 1; page < maxPage; page++)
            {
                var result = api.GetDreamUpdate(dream.Status["id"].StringData, page);
                //Get Extend Dream Info
                var dreamInfo = (JObject)result["dream"];
                var id = dreamInfo["id"].Value<string>();
                var title = dreamInfo["title"].Value<string>();
                foreach (var values in dreamInfo)
                {
                    StringItem valItem = null;
                    string targetValue = values.Value.Value<string>();
                    if (dream.Status.ContainsKey(values.Key))
                        valItem = dream.Status[values.Key];
                    else
                    {
                        valItem = new StringItem();
                        dream.Status[values.Key] = valItem;
                    }
                    if (valItem.StringData != targetValue)
                    {
                        DiffDetected?.Invoke(
                            "http://nian.so/#!/dream/" + id,
                            uName + "修改了梦想" + title + "的信息",
                            "属性" + values.Key + "从" + valItem.StringData + "变为" + targetValue,
                            "DreamList." + id + "." + values.Key);
                        valItem.StringData = targetValue;
                        valItem.LastModifiedTime = currentTime;
                    }
                }
            }
        }

        private void GetDreamList(int targetUser, NianData data)
        {
            int page = 1;
            if(data.Dreams==null)
                data.Dreams=new List<DreamInfo>();
            for (page = 1;
                page <= Math.Ceiling(
                    int.Parse(data.ListItems["dream"].StringData) / 20.0f);
                page++)
            {
                var result = api.GetUserDreams(targetUser.ToString(), page);
                foreach (var dreamStatus in (JArray) result["dreams"])
                {
                    var dreamInfo = (JObject) dreamStatus;
                    var id = dreamInfo["id"].Value<string>();
                    var title = dreamInfo["title"].Value<string>();
                    var di = data.Dreams.FirstOrDefault(
                        info => info.Status["id"].StringData == id);
                    if (di == null)
                    {
                        di=new DreamInfo();
                        data.Dreams.Add(di);
                        DiffDetected?.Invoke(
                            "http://nian.so/#!/dream/" + id,
                            uName + "新增了梦想" + title,
                            uName + "新增了梦想" + title,
                            "DreamList.ID");
                        di.Status=new Dictionary<string, StringItem>();
                    }
                    if (di.isRemoved == true)
                    {
                        di.Status["id"].IsRemoved = false;
                        DiffDetected?.Invoke(
                            "http://nian.so/#!/dream/" + id,
                            uName + "公开了梦想" + title,
                            uName + "公开了梦想" + title,
                            "DreamList.ID");
                    }
                    
                }
            }
        }


        public void Start()
        {
            db=new LiteDatabase(database);
            var col = db.GetCollection<NianData>();
            data=col.FindOne(Query.All());
            if (data == null)
            {
                data = new NianData();
                data.ListItems=new Dictionary<string, StringItem>();
                data.Dreams=new List<DreamInfo>();
                col.Insert(data);
            }
            api=new NianApi();
            bool loginFlag = false;
            if (File.Exists(session))
            {
                string[] data = File.ReadAllLines(session);
                if (api.RestoreLogin(data[0], data[1]))
                    loginFlag = true;
            }
            if (loginFlag == false && api.Login(UserName,PassWord))//Short Circuit
            {
                string uid = "";
                string shell = "";
                api.GetLoginToken(out uid, out shell);
                File.WriteAllText(session, uid + "\r\n" + shell);
            }
            else if(loginFlag==false)
            {
                throw new ArgumentException(Alias + ":无法登录!");
            }
            isCancel=new CancellationTokenSource();
            updateJob=new Task(()=> {Run(isCancel.Token);},isCancel.Token);
            updateJob.Start();
        }

        private void Run(CancellationToken token)
        {

            var col = db.GetCollection<NianData>();
            for (;;)
            {
                
                currentTime = DateTime.Now;
                //Status Data Compare
                var result = api.GetUserData(TargetUID.ToString())["user"] as JObject;
                uName = result["name"].Value<string>();
                foreach (var obj in result)
                {
                    var val = obj.Value as JValue;
                    if (val != null)
                    {
                        string past=null;
                        bool isUpdated=false;
                        if (data.ListItems.ContainsKey(obj.Key))
                        {
                            //Diff
                            var history = data.ListItems[obj.Key];
                            if (history.StringData != obj.Value.Value<string>())
                            {
                                isUpdated = true;
                                past = history.StringData;
                                history.StringData = obj.Value.Value<string>();
                                history.LastModifiedTime = DateTime.Now;
                                data.ListItems[obj.Key] = history;
                            }
                        }
                        else
                        {
                            isUpdated = true;
                            past = "";
                            data.ListItems[obj.Key]=new StringItem()
                            {
                                IsRemoved = false,
                                LastModifiedTime = DateTime.Now,
                                StringData = obj.Value.Value<string>()
                            };
                        }
                        if (isUpdated)
                        {
                            DiffDetected?.Invoke(
                                "http://nian.so/#!/user/" + TargetUID,
                                uName + "修改了" + obj.Key,
                                uName + "修改了" + obj.Key + ",从" + past + "变为" + obj.Value.Value<string>(),
                                "UserInfo." + obj.Key);
                        }
                    }
                }
                GetDreamList(TargetUID,data);
                col.Update(data);
                token.WaitHandle.WaitOne(Math.Max(60000, Interval));
                token.ThrowIfCancellationRequested();
            }
        }

        public void Stop()
        {
            isCancel.Cancel();
            try
            {
                updateJob.Wait();
            }
            catch (AggregateException e)
            {
                foreach (var v in e.InnerExceptions)
                    Console.WriteLine(e.Message + " " + v.Message);
            }
            finally
            {
                isCancel.Dispose();
            }
        }

        public void LoadDefaultSetting()
        {
            int randNum=new Random().Next(1,100000);
            Alias = "NianStalker" + randNum;
            Interval = 600000;
        }

        [STKDescription("当有数据更新之后")]
        public Action<string, string, string, string> DiffDetected { get; set; }
    }
}
