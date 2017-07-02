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
        private int privatePeroid => (1000 * 60 * 60 * 24) / Math.Max(Interval, 60000);
        private int currentPeroid;
        /// <summary>
        /// 对应数据项的增量抓取函数表
        /// </summary>

        private void GetDreamExtendInfo(DreamInfo dream, NianData data,bool Incremantal=true)
        {
            int maxPage = 1;
            //Origin Target
            int unresolvedDiff = 0;
            bool isAllResolved = false;
            int pos = dream.Steps.Count - 1;
            bool isFoundHead = false;//是否找到最近出现的元素
            List<StepInfo> pendingInsert=new List<StepInfo>();
            for (int page = 1; page <= maxPage; page++)
            {
                if (dream.Status.ContainsKey("private"))
                {
                    if (dream.Status["private"] == "2") continue; //被删除的记本
                    if (dream.Status["private"] == "1" && currentPeroid % privatePeroid != 0) continue;
                    //私密记本一天只能拉取1次
                }
                var result = api.GetDreamUpdate(dream.Status["id"], page);
                //Get Extend Dream Info
                
                var id = dream.Status["id"];
                var title = dream.Status["title"];
                if (page == 1)
                {
                    var dreamInfo = (JObject)result["dream"];
                    foreach (var values in dreamInfo)
                    {
                        string valItem = "";
                        if (values.Value is JArray) continue;
                        string targetValue = values.Value.Value<string>();
                        if (dream.Status.ContainsKey(values.Key))
                            valItem = dream.Status[values.Key];
                        if (values.Key == "step")
                        {
                            unresolvedDiff=
                                int.Parse(targetValue) - int.Parse("0" + valItem);
                        }
                        if (valItem != targetValue)
                        {
                            DiffDetected?.Invoke(
                                "http://nian.so/#!/dream/" + id,
                                uName + "修改了梦想" + title + "的信息",
                                "属性" + values.Key + "从" + valItem + "变为" + targetValue,
                                "DreamList." + id + "." + values.Key);
                            dream.Status[values.Key] = targetValue;
                        }
                    }
                    maxPage = (int)Math.Ceiling(int.Parse(dream.Status["step"]) / 20.0f);
                }
                if (unresolvedDiff == 0 && Incremantal)//需要翻到下一页才能完全保证,因为有同时删除和增加的情况
                {
                    if (isAllResolved)
                        break;
                    else
                        isAllResolved = true;
                }
                else
                    isAllResolved = false;
                var stepsArray = (JArray) result["steps"];
                foreach (var stepInfo in stepsArray)
                {
                    StepInfo si;
                    int unresolvedComment = 0;
                    var currentId = stepInfo["sid"].Value<string>();
                    int index = dream.Steps.FindIndex(item => item.Status["sid"] == currentId);
                    StepInfo refsi = dream.Steps.ElementAtOrDefault(index);
                    string sid = refsi?.Status["sid"];
                    if (index==-1)
                    {
                            if (isFoundHead)
                                throw new Exception("莫名的顺序,怀疑不对");
                            si = new StepInfo
                            {
                                Status = new Dictionary<string, string>(),
                                Images = new List<string>(),
                                Comments = new List<CommentInfo>()
                            };
                            pendingInsert.Add(si);
                            foreach (var val in (JObject)stepInfo)
                            {
                                if (val.Key == "images")
                                {
                                    foreach (var imgPath in (JArray)val.Value)
                                    {
                                        si.Images.Add(imgPath["path"].Value<string>());
                                    }
                                }
                                else
                                    si.Status[val.Key] = val.Value.Value<string>();
                            }
                            DiffDetected?.Invoke(
                                "http://nian.so/m/step/" + currentId,
                                uName + "在记本" + title + "发布了一条足迹",
                                "足迹内容:" + si.Status["content"],
                                "DreamList." + id + "." + currentId);
                            unresolvedComment = ((JObject) stepInfo)["comments"].Value<int>();
                            unresolvedDiff--;
                    }
                    else
                    {
                        isFoundHead = true;
                        if(pos-index <0)
                            throw new Exception("???WTF???");
                        for (int j = index + 1; j <= pos; j++)
                        {
                            if (!dream.Steps[j].isRemoved)
                            {
                                dream.Steps[j].isRemoved = true;
                                unresolvedDiff++;
                                DiffDetected?.Invoke(
                                    "http://nian.so/m/dream/" + dream.Status["id"],
                                    uName + "删除了一条在" + title + "的足迹",
                                    "足迹内容:" + dream.Steps[j].Status["content"],
                                    "DreamList." + id + "." + dream.Steps[j].Status["sid"]);
                            }
                        }
                        pos = index - 1;
                        unresolvedComment =
                                int.Parse(refsi.Status["comments"]) -
                                ((JObject)stepInfo)["comments"].Value<int>();
                        refsi.Status["comments"] = ((JObject) stepInfo)["comments"].Value<string>();
                        si = refsi;
                    }

                    //需要检测的只有comment
                    if (unresolvedComment != 0)
                    {
                        ResolveComment(si,unresolvedComment);
                    }
                }
            }
            if(unresolvedDiff!=0)
                Console.WriteLine("Still Have UnresolvedDiff!!!!");
            for (int j = pendingInsert.Count - 1; j >= 0; j--)
            {
                dream.Steps.Add(pendingInsert[j]);
            }
        }

        private void ResolveComment(StepInfo step, int unresolvedComments)
        {
            //不想写增量了!
            //不过不用增量的方法怎么检测删除比较好呢?
            //还是写增量逻辑吧Q Q
            bool isFoundHead = false;
            List<CommentInfo> pendingInsert=new List<CommentInfo>();
            bool isAllResolved = false;
            int pos = step.Comments.Count - 1;
            int maxPage = (int)Math.Ceiling(int.Parse(step.Status["comments"])/15.0f);
            string sid = step.Status["sid"];
            for (int page = 1; page <= maxPage; page++)
            {
                var result = api.GetComments(step.Status["sid"], page);
                var commentArray = (JArray) result["comments"];
                foreach (var comment in commentArray)
                {
                    string cid = comment["id"].Value<string>();
                    int index = step.Comments.FindIndex(item => item.Status["id"] == cid);
                    if (index == -1)
                    {
                        if(isFoundHead)
                            throw new Exception("这里顺序不对吧");
                        //CreateNew
                        CommentInfo cmt = new CommentInfo
                        {
                            Status = new Dictionary<string, string>()
                        };
                        pendingInsert.Add(cmt);
                        foreach (var val in (JObject)comment)
                        {
                            cmt.Status[val.Key] = val.Value.Value<string>();
                        }
                        DiffDetected?.Invoke(
                                    "http://nian.so/m/step/" + step.Status["sid"],
                                    uName + "的足迹下出现了一条新评论!",
                                    "足迹:" + step.Status["content"] + " 评论:" + cmt.Status["content"],
                                    "Step." + sid);
                        unresolvedComments--;
                    }
                    else
                    {
                        isFoundHead = true;
                        if(pos-index<0)
                            throw new Exception("??????WTF");
                        for (int j = index + 1; j <= pos; j++)
                        {
                            if (!step.Comments[j].isRemoved)
                            {
                                step.Comments[j].isRemoved = true;
                                unresolvedComments++;
                                DiffDetected?.Invoke(
                                    "http://nian.so/m/step/" + step.Status["sid"],
                                    uName + "的足迹下有一条评论被删除了!",
                                    "足迹:" + step.Status["content"] + " 评论:" + step.Comments[j].Status["content"],
                                    "Step." + sid);
                            }
                        }
                        pos = index - 1;
                    }
                }
                if (unresolvedComments == 0)
                {
                    if (isAllResolved)
                        break;
                    else
                        isAllResolved = true;
                }
                else
                    isAllResolved = false;
            }
        }

        private void GetDreamList(int targetUser, NianData data)
        {
            int page = 1;
            if(data.Dreams==null)
                data.Dreams=new List<DreamInfo>();
            for (page = 1;
                page <= Math.Ceiling(
                    int.Parse(data.ListItems["dream"]) / 20.0f);
                page++)
            {
                var result = api.GetUserDreams(targetUser.ToString(), page);
                foreach (var dreamStatus in (JArray) result["dreams"])
                {
                    var dreamInfo = (JObject) dreamStatus;
                    var id = dreamInfo["id"].Value<string>();
                    var title = dreamInfo["title"].Value<string>();
                    var di = data.Dreams.FirstOrDefault(
                        info => info.Status["id"] == id);
                    if (di == null)
                    {
                        di=new DreamInfo();
                        data.Dreams.Add(di);
                        DiffDetected?.Invoke(
                            "http://nian.so/#!/dream/" + id,
                            uName + "新增了梦想" + title,
                            uName + "新增了梦想" + title,
                            "DreamList.ID");
                        di.Status=new Dictionary<string, string>();
                        di.Steps = new List<StepInfo>();
                    }
                    foreach (var val in dreamInfo)
                    {
                        di.Status[val.Key] = val.Value.Value<string>();
                    }

                }
            }
            try
            {
                foreach (var dataDream in data.Dreams)
                {
                    GetDreamExtendInfo(dataDream, data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                data.ListItems=new Dictionary<string, string>();
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
            currentPeroid = 0;
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
                        var targetValue = obj.Value.Value<string>();
                        var sourceValue = "";
                        if (data.ListItems.ContainsKey(obj.Key))
                            sourceValue = data.ListItems[obj.Key];
                        if (sourceValue != targetValue)
                        {
                            data.ListItems[obj.Key] = targetValue;
                            DiffDetected?.Invoke(
                                "http://nian.so/#!/user/" + TargetUID,
                                uName + "修改了" + obj.Key,
                                uName + "修改了" + obj.Key + ",从" + sourceValue + "变为" + targetValue,
                                "UserInfo." + obj.Key);
                        }
                    }
                }
                GetDreamList(TargetUID,data);
                col.Update(data);
                currentPeroid++;
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
