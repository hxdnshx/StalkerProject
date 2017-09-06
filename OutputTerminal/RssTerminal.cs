using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Security.Cryptography;
using System.Text;
using SQLite.Net;

namespace StalkerProject.OutputTerminal
{
    public class RssTerminal : STKWorker
    {
        public int FeedId { get; set; }
        public string FeedName { get; set; }
        public string OutputTimeZone { get; set; }
        private TimeZoneInfo _timeZone;
        private SQLiteConnection database=null;
        private SyndicationFeed feed;

        protected override void Prepare() {
            base.Prepare();
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(OutputTimeZone);
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid Time Zone Name:" + OutputTimeZone);
                _timeZone = TimeZoneInfo.Local;
            }
            feed = new SyndicationFeed(FeedName, "Provided By StalkerProject",
                new Uri("http://127.0.0.1"), "id=" + FeedId.ToString(), DateTime.Now);
        }

        public void GetDatabase(SQLiteConnection db)
        {
            database = db;
        }

        static SHA256 hash = new SHA256Managed();
        public static string GetStringHash(string str)
        {
            {
                byte[] data = hash.ComputeHash(Encoding.Default.GetBytes(str));
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }

        protected override void Run() {
            base.Run();
            if (IsFirstRun)
            {
                if (database == null)
                    waitToken.WaitHandle.WaitOne(10000); //WaitFor 10 seconds
                if (database == null)
                {
                    Console.WriteLine("No DiffDatabase connected,Service Terminate");
                }
            }
            DateTime updateTime = DateTime.Now;
            //Rebuild RssData
            try
            {
                var iter = (from p in database.Table<DiffData>()
                            orderby p.OutputTime descending
                            select p).Take(50);
                List<SyndicationItem> item = new List<SyndicationItem>();
                bool isFirst = true;
                foreach (var val in iter)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        updateTime = val.OutputTime;
                    }
                    var destTime = TimeZoneInfo.ConvertTime(val.OutputTime, _timeZone);
                    SyndicationItem sitem = new SyndicationItem()
                    {
                        Title = new TextSyndicationContent(val.Summary),
                        //Summary = SyndicationContent.CreatePlaintextContent(val.Summary),
                        Content = SyndicationContent.CreateHtmlContent(val.Content),
                        PublishDate = destTime,
                        LastUpdatedTime = destTime,
                        Links = { new SyndicationLink(new Uri(val.RelatedAddress)) },
                        Id = GetStringHash(val.Summary)
                    };
                    item.Add(sitem);
                }
                feed.Items = item;
                feed.LastUpdatedTime = updateTime;
                //Console.WriteLine("RssData Updated");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                string outputstr = e.ToString() + "\n";
                string anotherPart = "模块RssTerminal发生了异常!\n"
                                     + e.StackTrace
                                     + "\n"
                                     + e.InnerException;
                File.AppendAllText("ErrorDump.txt", outputstr + anotherPart);
            }
        }

        public override void LoadDefaultSetting()
        {
            base.LoadDefaultSetting();
            int randResult = new Random().Next(1, 1000000);
            FeedId = randResult;
            Alias = "RssTerminal" + randResult;
            FeedName = "RSS输出-" + randResult;
            Interval = 1200000;
            OutputTimeZone = "China Standard Time";
        }

        [STKDescription("输出RSS信息")]
        public void DisplayRss(HttpListenerContext context,string subUrl)
        {
            feed.Links.Clear();
            feed.Links.Add(SyndicationLink.CreateSelfLink(context.Request.Url));//.BaseUri = context.Request.Url;
            using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                XmlWriter xmlWriter = XmlWriter.Create(writer);
                feed.SaveAsAtom10(xmlWriter);
                xmlWriter.Close();
                context.Response.Close();
            }
        }

        
    }
}
