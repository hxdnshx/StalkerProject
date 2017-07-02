using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LiteDB;
using StalkerProject;

namespace OutputTerminal
{
    public class RssTerminal : ISTKService
    {
        public int Interval { get; set; }
        public string Alias { get; set; }
        public int FeedId { get; set; }
        public string FeedName { get; set; }
        private LiteDatabase database;
        private SyndicationFeed feed;
        private Task updateJob;
        private CancellationTokenSource isCancel;
        public void Start()
        {
            database=new LiteDatabase(Alias + ".db");
            feed=new SyndicationFeed(FeedName,"Provided By StalkerProject",
                new Uri("http://127.0.0.1"),"id=" + FeedId.ToString(),DateTime.Now);
            isCancel=new CancellationTokenSource();
            updateJob=new Task(() => { UpdateLoop(isCancel.Token);},isCancel.Token);
            updateJob.Start();
        }

        public void UpdateLoop(CancellationToken token)
        {
            var col = database.GetCollection<OutputData>();
            for (;;)
            {
                token.WaitHandle.WaitOne(Math.Max(60000,Interval));
                token.ThrowIfCancellationRequested();
                //Rebuild RssData
                try
                {
                    var iter = col.Find(Query.All(Query.Descending), limit: 20);
                    List<SyndicationItem> item = new List<SyndicationItem>();
                    foreach (var val in iter)
                    {
                        SyndicationItem sitem = new SyndicationItem()
                        {
                            Title = new TextSyndicationContent(val.Summary),
                            Summary = SyndicationContent.CreatePlaintextContent(val.Summary),
                            Content = SyndicationContent.CreatePlaintextContent(val.Content),
                            PublishDate = val.OutputTime
                        };
                        item.Add(sitem);
                    }
                    feed.Items = item;
                    Console.WriteLine("RssData Updated");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
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
            int randResult = new Random().Next(1, 1000000);
            FeedId = randResult;
            Alias = "RssTerminal" + randResult;
            FeedName = "RSS输出-" + randResult;
            Interval = 1200000;
        }

        [STKDescription("输出RSS信息")]
        public void DisplayRss(HttpListenerContext context)
        {
            feed.BaseUri = context.Request.Url;
            using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                XmlWriter xmlWriter = XmlWriter.Create(writer);
                feed.SaveAsRss20(xmlWriter);
                xmlWriter.Close();
                context.Response.Close();
            }
        }

        [STKDescription("录入新的数据")]
        public void InputData(string RelatedAddress, string Summary, string Content, string RelatedVar)
        {
            var col=database.GetCollection<OutputData>();
            col.Insert(new OutputData()
            {
                RelatedAddress = RelatedAddress,
                Summary = Summary,
                Content = Content,
                RelatedVar = RelatedVar,
                OutputTime = DateTime.Now
            });
            
        }
    }
}
