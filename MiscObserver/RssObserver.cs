using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data.SQLite;
using System.IO;

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
        public static void ExecCommand(this SQLiteConnection conn, string command,
            Dictionary<string, object> parameters = null)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = command;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new SQLiteParameter(param.Key, param.Value));
                    }
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static DataTable ExecQuery(this SQLiteConnection conn, string query,
            Dictionary<string, object> parameters = null)
        {
            DataTable dt = new DataTable();
            using (var cmd = new SQLiteCommand(conn))
            {
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                cmd.CommandText = query;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new SQLiteParameter(param.Key, param.Value));
                    }
                }
                adapter.Fill(dt);
            }
            return dt;
        }
    }
    public class RssObserver : STKWorker
    {
        class RSSData
        {
            public string GUID { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime PubTime { get; set; }
        }
        public string URL { get; set; }
        private SQLiteConnection _conn;

        protected override void Prepare()
        {
            _conn = CreateConnectionForSchemaCreation(Alias + ".db");
        }

        public static SyndicationFeed GetFeed(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                var ff = new Rss20FeedFormatter(); // for Atom you can use Atom10FeedFormatter()
                var xr = XmlReader.Create(uri);
                ff.ReadFrom(xr);
                return ff.Feed;
            }
            return null;
        }

        public SQLiteConnection CreateConnectionForSchemaCreation(string fileName)
        {
            bool isNew = !File.Exists(fileName);
            var conn = new SQLiteConnection();
            conn.ConnectionString = new DbConnectionStringBuilder()
            {
                {"Data Source", fileName},
                {"Version", "3"},
                {"FailIfMissing", "False"},
            }.ConnectionString;
            conn.Open();
            if (isNew)
            {
                using (SQLiteTransaction trans = conn.BeginTransaction())
                {
                    conn.ExecCommand(
                        "CREATE TABLE FeedData (GUID TEXT UNIQUE, Title TEXT, Desc TEXT, PubTime INTEGER);" + 
                        "CREATE INDEX idx_PubTime ON FeedData(PubTime);");
                    trans.Commit();
                }
            }
            return conn;
        }

        protected override void Run()
        {
            base.Run();
            List<string> historyFeeds = new List<string>();
            var dt = _conn.ExecQuery("SELECT GUID FROM FeedData ORDER BY PubTime DESC LIMIT 30");
            foreach (DataRow dataRow in dt.Rows)
            {
                historyFeeds.Add(dataRow["GUID"].ToString());
            }
            SyndicationFeed feed;
            try
            {
                feed = GetFeed(URL);
            }
            catch (Exception e)
            {
                //Network Error
                Console.Write("Unable to Get Feed:" + URL);
                return;
            }
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
        }

        public override void LoadDefaultSetting()
        {
            int randInt = new Random().Next(1, 100000);
            Alias = "RssObserver" + randInt.ToString();
            Interval = 3600000;
        }

        public Action<string, string, string, string> DiffDetected { get; set; }
    }
}
