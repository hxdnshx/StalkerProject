using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;

namespace StalkerProject.OutputTerminal
{
    /// <summary>
    /// 过去使用LiteDB时的数据结构
    /// </summary>
    class OutputData
    {
        //public ObjectId Id { get; set; }
        public string RelatedAddress { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string RelatedVar { get; set; }
        public DateTime OutputTime { get; set; }
    }

    [Table("DiffData")]
    class DiffData
    {
        [PrimaryKey][AutoIncrement]
        public int Id { get; set; }
        public string RelatedAddress { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string RelatedVar { get; set; }
        [Indexed]
        public DateTime OutputTime { get; set; }
    }
}
