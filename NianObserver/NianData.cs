using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace StalkerProject.NianObserver
{
    public class NianData
    {
        public ObjectId Id { get; set; }
        /// <summary>
        /// 用于存放始终存在的数据：粉丝，关注等
        /// </summary>
        public Dictionary<string,StringItem> ListItems { get; set; }
        public List<DreamInfo> Dreams { get; set; }
    }

    public class DreamInfo
    {
        public ObjectId Id { get; set; }
        public Dictionary<string, StringItem> Status { get; set; }
        [BsonIgnore]
        public bool isRemoved => Status.FirstOrDefault(item => item.Key == "id").Value?.IsRemoved == true;

    }

    public class StepsInfo
    {
        public ObjectId Id { get; set; }
        
    }
}
