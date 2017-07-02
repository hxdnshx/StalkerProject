using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace StalkerProject.NianObserver
{
    /*
     * 对思考的记录:
     * 之前ListItem,Status项都是采用在DataHelper中定义的
     * StringItem进行保存的,会附带上"当前是否还存在","最后变更日期"
     * 两个数据
     * 实际上算是跟分析网易云音乐时候的思想相混淆了
     * 这里会被删除的东西,从粒度上而言,只有"梦想,进展,评论"
     * 三个,也就没有必要对每个变量都存储这些东西了.
     * 
     * 追记:你念真是厉害啊,厉害啊
     * 梦想ID永远不会被删除
     * private:0 公开
     * private:1 私密 可通过api获取
     * private:2 删除
     * 
     * 顺便一说,step删除了之后在系统中也是可见的
     * (特别有毒
     * 
     */
    public class NianData
    {
        public ObjectId Id { get; set; }
        /// <summary>
        /// 用于存放始终存在的数据：粉丝，关注等
        /// </summary>
        public Dictionary<string,string> ListItems { get; set; }
        public List<DreamInfo> Dreams { get; set; }
    }

    public class DreamInfo
    {
        public ObjectId Id { get; set; }
        public Dictionary<string, string> Status { get; set; }
        //public bool isRemoved { get; set; }
        public List<StepInfo> Steps { get; set; }
    }

    public class StepInfo
    {
        public ObjectId Id { get; set; }
        public List<string> Images { get; set; }
        public Dictionary<string,string> Status { get; set; }
        public List<CommentInfo> Comments { get; set; }
        /*
         * StepInfo中,因为会影响到增量计数,所以还是加上isRemoved标识了
         */
        public bool isRemoved { get; set; }
    }

    public class CommentInfo
    {
        public ObjectId Id { get; set; }
        public Dictionary<string,string> Status { get; set; }
        public bool isRemoved { get; set; }
    }
}
