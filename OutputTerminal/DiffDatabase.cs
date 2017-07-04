using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace StalkerProject.OutputTerminal
{
    class DiffDatabase : ISTKService
    {
        public string Alias { get; set; }
        public string DatabasePath { get; set; }
        private LiteDatabase database;
        public void Start()
        {
            if (string.IsNullOrWhiteSpace(DatabasePath))
                DatabasePath = Alias + ".db";
            Console.WriteLine("Created" + DatabasePath);
            database=new LiteDatabase(DatabasePath);
            var col = database.GetCollection<OutputData>();
            Console.WriteLine("Col" + col.ToString());
            col.Insert(new OutputData()
            {
                RelatedAddress = "111",
                Summary = "222",
                Content = "333",
                RelatedVar = "444",
                OutputTime = DateTime.Now
            });
            DatabaseSource?.Invoke(database);
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
        public Action<LiteDatabase> DatabaseSource { get; set; }
        
        [STKDescription("录入新的数据")]
        public void InputData(string RelatedAddress, string Summary, string Content, string RelatedVar)
        {
            //var trans = database.BeginTrans();
                
                Console.WriteLine("Inserted");
            //trans.Commit();
        }
    }
}
