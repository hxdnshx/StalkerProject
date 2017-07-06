using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json.Linq;

namespace StalkerProject.NetEaseObserver
{
    /*
     * 虽然说加Attribute再加Interface好像可以实现代码自动转换，不过现在却丝毫不想那样做
     * 在对念的数据结构进行构造后
     * 感觉驾轻就熟了许多啊w
     * 接下来就开始分析网易云音乐的部分吧
     * ((总觉得网易云的抓取十分消耗内存,有点害怕
     * 可能重点会在Freq数据的分析上
     * 这里如果不弄好算法的话就没什么意义了?
     * (似乎3个list就能解决的事情)
     * (时间复杂度O(N)...空间复杂度O(N))
     * 所以说在骗自己很复杂么((笑
     * Events用增量
     * Fans用增量
     * Follows用增量
     * PlayList信息用Dictionary如何?会省好多事情
     * 要做一个通用的增量算法辅助函数呢w
     */
    public class NetEaseData
    {
        public ObjectId Id { get; set; }
        /// <summary>
        /// 用于存放始终存在的数据：粉丝，关注等
        /// </summary>
        public Dictionary<string,string> ListItems { get; set; }
        public List<EventData> Events { get; set; }
        public List<RemovableString> Follows { get; set; }
        public List<RemovableString> Fans { get; set; }
        public string WeeklyFreq { get; set; }
        public string AllFreq { get; set; }
        public List<PlayList> PlayLists { get; set; }

        public void ApplyNotNull()
        {
            if(ListItems==null)ListItems=new Dictionary<string, string>();
            if(Follows==null)Follows=new List<RemovableString>();
            if(Fans==null)Fans=new List<RemovableString>();
            if (WeeklyFreq == null) WeeklyFreq = "";
            if(AllFreq==null)AllFreq="";
            if(PlayLists==null)PlayLists=new List<PlayList>();
        }
    }

    public class EventData : IRemoveFlag
    {
        public ObjectId Id { get; set; }
        public string SongName { get; set; }
        public string SongArtist { get; set; }
        public string Comment { get; set; }
        public string RelatedLink { get; set; }

        public static EventData FromJson(JObject obj)
        {
            return new EventData()
            {
                SongName = obj["songName"].Value<string>(),
                SongArtist = obj["songArtist"].Value<string>(),
                Comment = obj["comment"].Value<string>(),
                RelatedLink = obj["href"].Value<string>()
            };
        }

        public bool IsRemoved { get; set; }
    }

    public class FreqItem
    {
        public ObjectId Id { get; set; }
        public string SongName { get; set; }
        public string SongArtist { get; set; }
        public int Percent { get; set; }

        public static FreqItem FromJson(JObject obj)
        {
            return new FreqItem()
            {
                SongName = obj["songName"].Value<string>(),
                SongArtist = obj["songArtist"].Value<string>(),
                Percent = obj["percent"].Value<int>()
            };
        }
    }

    /*
     *  {
            "id": "779285441",
            "playList": "Violetium",
            "playCount": "6",
            "description": "",
            "favCount": "0",
            "commentCount": "评论",
            "musicList": [
                "Intro",
                "I'm in Rapture (2016 Rework) ft nayuta",
                "Reset the World (Original Mix)",
                "O(Escapers) ft darkxixin",
                "Jasminum Sambac",
                "祀",
                "Digital Nonentity (Original Mix)",
                "Skysurfing",
                "Violet at Dawn",
                "Glittering Wave",
                "Sunset at the Beach",
                "Memory Conflict"
            ]
        }
     * */
    public class PlayList : IRemoveFlag
    {
        public Dictionary<string,string> ListItems { get; set; }
        public List<SongInfo> MusicList { get; set; }
        public bool IsRemoved { get; set; }
    }

    public class SongInfo : RemovableString
    {
        public int SongId { get; set; }
    }

    public class RemovableString : IRemoveFlag
    {
        public ObjectId Id { get; set; }
        public string Value { get; set; }
        public bool IsRemoved { get; set; }
        public static explicit operator string(RemovableString str)
        {
            return str.Value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
