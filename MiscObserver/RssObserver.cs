using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SQLite.Net;
using System.IO;
using System.Threading;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using SQLite.Net.Platform.Win32;

namespace StalkerProject.MiscObserver
{
    public static class SQLiteHelper
    {
        public static DateTime FromUnixTime(this long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalSeconds);
        }
    }

    class MyXmlReader : XmlTextReader
    {
        private readonly string _customUtcDateTimeFormat = "ddd, dd MMM yyyy HH:MM:SS GMT"; // Wed Oct 07 08:00:07 GMT 2009

        public MyXmlReader(Stream s) : base(s) { }

        public MyXmlReader(string inputUri, string customTimeFormat = "") : base(inputUri)
        {
            _customUtcDateTimeFormat = customTimeFormat;
        }

        public override string ReadElementContentAsString()
        {
            string nodeName = this.LocalName;
            string data = base.ReadElementContentAsString();
            if (nodeName == "pubDate")
            {
                DateTime dt;
                if (!DateTime.TryParse(data, out dt))
                    dt = DateTime.ParseExact(data, _customUtcDateTimeFormat, CultureInfo.InvariantCulture);
                string result = dt.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'Z'", CultureInfo.InvariantCulture);
                //实际做了这种事情：Sun, 20 Aug 2017 05:12:00 GMT => Sun, 20 Aug 2017 05:12:00 +08:00 (当转换为ddd, dd MMM yyyy HH:mm:ss zzz)
                //实际做了这种事情：Sun, 20 Aug 2017 04:01:00 GMT To Sun, 20 Aug 2017 04:01:00 Z(当转换为ddd, dd MMM yyyy HH:mm:ss 'Z')
                return result;
            }
            return data;
        }
    }

    public class RssObserver : STKWorker
    {
        [Table("FeedData")]
        class RSSData
        {
            [Unique]
            public string GUID { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            [Indexed]
            public DateTime PubTime { get; set; }
        }
        public string URL { get; set; }
        public string CustomTimeFormat { get; set; }
        private SQLiteConnection _conn;

        protected override void Prepare()
        {
            _conn = CreateConnectionForSchemaCreation(Alias + ".db");
        }

        public static SyndicationFeed GetFeed(string uri, string timeFormat = "")
        {
            if (!string.IsNullOrEmpty(uri))
            {
                var ff = new Rss20FeedFormatter(); // for Atom you can use Atom10FeedFormatter()
                var xr = new MyXmlReader(uri,timeFormat);
                ff.ReadFrom(xr);
                return ff.Feed;
            }
            return null;
        }

        public SQLiteConnection CreateConnectionForSchemaCreation(string fileName)
        {
            var conn = new SQLiteConnection(
                new SQLitePlatformGeneric()
                , fileName);
            conn.CreateTable<RSSData>();
            return conn;
        }

        [Table("FeedData")]
        class FeedGUID
        {
            public string GUID { get; set; }
        }

        protected override void Run()
        {
            base.Run();
            if (IsFirstRun) {
                this.waitToken.WaitHandle.WaitOne(new Random().Next(0,2000000));
            }
            List<string> historyFeeds = new List<string>();
            var result = _conn.Query<FeedGUID>("SELECT GUID FROM FeedData ORDER BY PubTime DESC LIMIT 30");
            foreach (var val in result)
            {
                historyFeeds.Add(val.GUID);
            }
            SyndicationFeed feed;
            try
            {
                feed = GetFeed(URL,CustomTimeFormat);
            }
            catch (Exception e)
            {
                //Network Error
                Console.WriteLine(e);
                Console.WriteLine("Unable to Get Feed:" + URL);
                return;
            }
            string title = feed.Title.Text;
            _conn.RunInTransaction(() =>
            {
                foreach (var synItem in feed.Items.Reverse())
                {
                    if (historyFeeds.IndexOf(synItem.Id) != -1) continue;
                    try
                    {
                        _conn.Insert(new RSSData
                        {
                            Description = synItem.Summary.Text,
                            GUID = synItem.Id,
                            Title = synItem.Title.Text,
                            PubTime = synItem.PublishDate.DateTime.ToUniversalTime()
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to Insert data:\n{synItem.Id}\n{synItem.Title.Text}\n{synItem.Summary.Text}\n{synItem.PublishDate}");
                        Console.WriteLine("Error info:" + e);
                        continue;
                        //此处违反约束的原因是重复的Id，所以不应该更新数据
                        //因为其实数据约束也就这一个嘛
                    }
                    DiffDetected?.Invoke(synItem.Id, title + " - " + synItem.Title.Text, synItem.Summary.Text, Alias + ".Updated");
                }
            });
            /*
            using (SQLiteTransaction trans = _conn.BeginTransaction())
            {
                foreach (var synItem in feed.Items.Reverse())
                {
                    if (historyFeeds.IndexOf(synItem.Id) != -1) continue;
                    try
                    {
                        _conn.ExecCommand(
                            "INSERT INTO FeedData(GUID,Title,Desc, PubTime) VALUES(@GUID,@Title,@Desc,@PubTime)",
                            new Dictionary<string, object>
                            {
                                {"@GUID", synItem.Id},
                                {"@Title", synItem.Title.Text},
                                {"@Desc", synItem.Summary.Text},
                                {"@PubTime", synItem.PublishDate.DateTime.ToUnixTime()}
                            });
                    }
                    catch (Exception e)
                    {
                        //Another change will modify even if some commands fail.
                        Console.WriteLine(e);
                    }
                    DiffDetected?.Invoke(synItem.Id,synItem.Title.Text,synItem.Summary.Text,Alias + ".Updated");
                }
                trans.Commit();
            }
            */
        }

        public override void LoadDefaultSetting()
        {
            base.LoadDefaultSetting();
            int randInt = new Random().Next(1, 100000);
            Alias = "RssObserver" + randInt.ToString();
            Interval = 3600000;
            CustomTimeFormat = "ddd, dd MMM yyyy HH:MM:SS GMT";
        }

        public Action<string, string, string, string> DiffDetected { get; set; }
    }
}
