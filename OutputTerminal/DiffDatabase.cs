using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;

namespace StalkerProject.OutputTerminal
{

    public class DiffDatabase : ISTKService
    {
        public string Alias { get; set; }
        public string DatabasePath { get; set; }
        private SQLiteConnection database;
        public void Start()
        {
            if (string.IsNullOrWhiteSpace(DatabasePath))
                DatabasePath = Alias + ".db";
            database = CreateDb(DatabasePath);
            DatabaseSource?.Invoke(database);
        }

        public SQLiteConnection CreateDb(string fileName) {
            var platform = new SQLitePlatformGeneric();
            platform.SQLiteApi.Config(ConfigOption.Serialized);
            /*
             * 使用Serialize模式串行地进行数据读取写入.
             * 如果要并行,需要将DatabaseSource的传递变为string
             * 由各个使用到这个数据库的地方自行建立SQLiteConnection
             * (*MultiThread模式下不能让同一个SQLiteConnection同时被多个线程使用)
            */
            var conn = new SQLiteConnection(
                platform
                , fileName);
            conn.CreateTable<DiffData>();
            return conn;
        }

        public void Stop()
        {
         
        }

        public void LoadDefaultSetting()
        {
            int randInt=new Random().Next(1,100000);
            Alias = "DiffDatabase" + randInt;
        }

        /*
         * 注意，调用这个的时候不保证Start已经被运行
         */
        [STKDescription("数据源设置")]
        public Action<SQLiteConnection> DatabaseSource { get; set; }


        [STKDescription("录入新的数据")]
        public void InputData(string RelatedAddress, string Summary, string Content, string RelatedVar)
        {
            database.RunInTransaction(() => {
                database.Insert(new DiffData {
                    RelatedAddress = RelatedAddress,
                    Summary = Summary,
                    Content = Content,
                    RelatedVar = RelatedVar,
                    OutputTime = DateTime.Now.ToUniversalTime()
                });
            });
        }
    }
}
