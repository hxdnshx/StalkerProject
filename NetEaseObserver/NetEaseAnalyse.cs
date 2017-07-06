using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Newtonsoft.Json.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace StalkerProject.NetEaseObserver
{
    class NetEaseAnalyse : ISTKService
    {
        public string Alias { get; set; }
        private string databaseDir => Alias + ".db";
        private LiteDatabase database;
        private NetEaseData data;
        private bool firstRun;


        public void Start()
        {
            database=new LiteDatabase(databaseDir);
            //Nothing to do...
            var col = database.GetCollection<NetEaseData>();
            data = col.FindOne(Query.All());
            if (data == null)
            {
                data = new NetEaseData();
                data.ListItems=new Dictionary<string, string>();
                data.AllFreq="";
                data.Events=new List<EventData>();
                data.Fans=new List<RemovableString>();
                data.Follows=new List<RemovableString>();
                data.PlayLists=new List<PlayList>();
                data.WeeklyFreq="";
                col.Insert(data);
                firstRun = true;
            }
        }

        public void Stop()
        {
            //Nothing to do
        }

        public void LoadDefaultSetting()
        {
            Alias = "NetEaseAnalyse" + new Random().Next(1, 10000);
        }

        

        [STKInputPort]
        public void OnDataUpdated(JObject newData)
        {
            var tmpData = DiffDetected;
            if (firstRun)
            {
                DiffDetected = null;
            }
            var col = database.GetCollection<NetEaseData>();
            var uName = newData["status"]["name"].Value<string>();
            var uid = newData["status"]["uid"].Value<string>();
            foreach (var prop in (JObject)newData["status"])
            {
                string sourceValue;
                data.ListItems.TryGetValue(prop.Key, out sourceValue);
                var targetValue = prop.Value.Value<string>();
                if (sourceValue != targetValue)
                {
                    DiffDetected?.Invoke(
                        "http://music.163.com/#/user/home?id=" + uid,
                        uName + "修改了" + prop.Key,
                        uName + "修改了" + prop.Key + ",从" + sourceValue + "变为" + targetValue,
                        "UserInfo." + prop.Key);
                    data.ListItems[prop.Key] = targetValue;
                }
            }
            {
                int pos = -1;
                bool isHeadFound = false;
                DataHelper.IncrementalLoop(ref pos, ref isHeadFound,
                    (JArray) newData["shares"].AsJEnumerable(),
                    data.Events,
                    shareInfo => EventData.FromJson((JObject) shareInfo), (a, b) => a.RelatedLink == b.RelatedLink, modifiedData =>
                    {
                        DiffDetected?.Invoke(
                            "http://music.163.com/#" + modifiedData.RelatedLink,
                            uName + "删除了歌曲分析：" + modifiedData.SongArtist,
                            string.Format("{0} - {1}\n{2}", modifiedData.SongName, modifiedData.SongArtist,
                                modifiedData.Comment),
                            "UserInfo." + uid + ".Event");
                    }, modifiedData =>
                    {
                        DiffDetected?.Invoke(
                            "http://music.163.com/#" + modifiedData.RelatedLink,
                            uName + "分享了一首好听的歌曲：" + modifiedData.SongArtist,
                            string.Format("{0} - {1}\n{2}", modifiedData.SongName, modifiedData.SongArtist,
                                modifiedData.Comment),
                            "UserInfo." + uid + ".Event");
                    });
            }
            {
                int pos = -1;
                bool isFoundHead = false;
                DataHelper.IncrementalLoop(ref pos,ref isFoundHead,
                    (JArray)newData["follows"].AsJEnumerable(),
                    data.Follows,
                    follow=>new RemovableString() {Value = follow.Value<string>()},
                    (a,b)=>!a.IsRemoved && !b.IsRemoved && a.Value.Equals(b.Value),
                    info =>
                    {
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/follows?id=" + uid,
                            uName + "取消关注了一位小姐姐：" + info.Value,
                            uName + "取消关注了一位小姐姐：" + info.Value,
                            "UserInfo." + uid + ".Fans"
                        );
                    },
                    info =>
                    {
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/follows?id=" + uid,
                            uName + "关注了一位小姐姐：" + info.Value,
                            uName + "关注了一位小姐姐：" + info.Value,
                            "UserInfo." + uid + ".Follow"
                            );
                    }
                    
                    );
            }
            {
                int pos = -1;
                bool isFoundHead = false;
                DataHelper.IncrementalLoop(ref pos, ref isFoundHead,
                    (JArray)newData["fans"].AsJEnumerable(),
                    data.Fans,
                    follow => new RemovableString() { Value = follow.Value<string>() },
                    (a, b) => !a.IsRemoved && !b.IsRemoved && a.Value.Equals(b.Value),
                    info =>
                    {
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/fans?id=" + uid,
                            uName + "的粉丝减少了一位：" + info.Value,
                            uName + "的粉丝减少了一位：" + info.Value,
                            "UserInfo." + uid + ".Fans"
                        );
                    },
                    info =>
                    {
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/fans?id=" + uid,
                            uName + "增加了一名新粉丝：" + info.Value,
                            uName + "增加了一名新粉丝：" + info.Value,
                            "UserInfo." + uid + ".Follow"
                        );
                    }
                );
            }
            if (newData["freqWeekly"] != null && ((JArray)newData["freqWeekly"]).Count != 0)
            {
                string srcStr = string.Concat(((JArray) newData["freqWeekly"]).AsJEnumerable()
                    .Select(info => info["songName"].Value<string>() + " - " + info["songArtist"].Value<string>() + "\n"));
                var diffBuilder = new InlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildDiffModel(data.WeeklyFreq ?? "",srcStr);
                int ranking = 1;
                //0-----4------12------20
                // Flag  OldRank NewRank
                Dictionary<string,int> changedSongs=new Dictionary<string, int>();
                foreach (var result in diff.Lines)
                {
                    int val = 0;
                    changedSongs.TryGetValue(result.Text, out val);
                    switch (result.Type)
                    {
                            case ChangeType.Inserted:
                                changedSongs[result.Text] = val | 1 | (ranking << 12);
                                ranking++;
                                break;
                            case ChangeType.Deleted:
                                changedSongs[result.Text] = val | 2 | (ranking << 4);
                                break;
                        default:
                            ranking++;
                            break;
                    }
                }
                foreach (var changedSong in changedSongs)
                {
                    if ((changedSong.Value & 3) == 1)
                    {
                        //Insert Only
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/home?id=" + uid,
                            uName + "的歌曲周榜新增了曲目" + changedSong.Key,
                            "曲目排名为:" + ((changedSong.Value & 0xFF000) >> 12).ToString(),
                            "UserInfo." + uid + ".WeekRanking");
                    }
                    else if ((changedSong.Value & 3) == 2)
                    {
                        //Remove Only
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/home?id=" + uid,
                            uName + "的歌曲周榜移除了曲目" + changedSong.Key,
                            "曲目排名为:" + ((changedSong.Value & 0xFF0) >> 4).ToString(),
                            "UserInfo." + uid + ".WeekRanking");
                    }
                    else
                    {
                        //Insert And Remove/
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/home?id=" + uid,
                            uName + "的歌曲周榜的曲目" + changedSong.Key + "排名发生了变化",
                            "原曲目排名为:" + ((changedSong.Value & 0xFF0) >> 4).ToString() + "变为:"
                            + ((changedSong.Value & 0xFF000) >> 12).ToString(),
                            "UserInfo." + uid + ".WeekRanking");
                    }
                }
                data.WeeklyFreq = srcStr;
            }
            if(newData["freqAll"]!=null && ((JArray)newData["freqAll"]).Count!=0)
            {
                string srcStr = string.Concat(((JArray)newData["freqAll"]).AsJEnumerable()
                    .Select(info => info["songName"].Value<string>() + " - " + info["songArtist"].Value<string>() + "\n"));
                var diffBuilder = new InlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildDiffModel(data.WeeklyFreq ?? "", srcStr);
                int ranking = 1;
                //0-----4------12------20
                // Flag  OldRank NewRank
                Dictionary<string, int> changedSongs = new Dictionary<string, int>();
                foreach (var result in diff.Lines)
                {
                    int val = 0;
                    changedSongs.TryGetValue(result.Text, out val);
                    switch (result.Type)
                    {
                        case ChangeType.Inserted:
                            changedSongs[result.Text] = val | 1 | (ranking << 12);
                            ranking++;
                            break;
                        case ChangeType.Deleted:
                            changedSongs[result.Text] = val | 2 | (ranking << 4);
                            break;
                        default:
                            ranking++;
                            break;
                    }
                }
                foreach (var changedSong in changedSongs)
                {
                    if ((changedSong.Value & 3) == 1)
                    {
                        //Insert Only
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/home?id=" + uid,
                            uName + "的歌曲榜新增了曲目" + changedSong.Key,
                            "曲目排名为:" + ((changedSong.Value & 0xFF000) >> 12).ToString(),
                            "UserInfo." + uid + ".AllRanking");
                    }
                    else if ((changedSong.Value & 3) == 2)
                    {
                        //Remove Only
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/home?id=" + uid,
                            uName + "的歌曲榜移除了曲目" + changedSong.Key,
                            "曲目排名为:" + ((changedSong.Value & 0xFF0) >> 4).ToString(),
                            "UserInfo." + uid + ".AllRanking");
                    }
                    else
                    {
                        //Insert And Remove/
                        DiffDetected?.Invoke(
                            "http://music.163.com/#/user/home?id=" + uid,
                            uName + "的歌曲榜的曲目" + changedSong.Value + "排名发生了变化",
                            "原曲目排名为:" + ((changedSong.Value & 0xFF0) >> 4).ToString() + "变为:"
                            + ((changedSong.Value & 0xFF000) >> 12).ToString(),
                            "UserInfo." + uid + ".AllRanking");
                    }
                }
                data.AllFreq = srcStr;
            }
            {
                var playListRoot = (JArray)newData["playLists"];
                foreach (var playList in playListRoot)
                {
                    var id = playList["id"].Value<string>();
                    var playListName = playList["playList"].Value<string>();
                    var currentList = data.PlayLists.FirstOrDefault(lst => lst.ListItems["id"] == id);
                    if (currentList == null)
                    {
                        currentList = new PlayList
                        {
                            MusicList = new List<SongInfo>(),
                            ListItems = new Dictionary<string, string>()
                        };
                        data.PlayLists.Add(currentList);
                    }
                    foreach (var prop in (JObject)playList)
                    {
                        if (prop.Key == "musicList") continue;
                        string sourceValue;
                        currentList.ListItems.TryGetValue(prop.Key, out sourceValue);
                        var targetValue = prop.Value.Value<string>();
                        if (sourceValue != targetValue)
                        {
                            DiffDetected?.Invoke(
                                "http://music.163.com/#/user/home?id=" + uid,
                                uName + "修改了播放列表" + playListName + "的属性" + prop.Key,
                                uName + "修改了" + prop.Key + ",从" + sourceValue + "变为" + targetValue,
                                "UserInfo." + id + "." + prop.Key);
                            currentList.ListItems[prop.Key] = targetValue;
                        }
                    }
                    int pos = -1;
                    bool IsFoundHead = false;
                    DataHelper.IncrementalLoop(ref pos,ref IsFoundHead,(JArray)playList["musicList"],currentList.MusicList,
                        src=>new SongInfo() {Value = src["songName"].Value<string>(),SongId = src["id"].Value<int>()},
                        (a, b) => !a.IsRemoved && !b.IsRemoved && a.SongId==b.SongId,
                        music =>
                        {
                            DiffDetected?.Invoke(
                                "http://music.163.com/#/playlist?id=" + id,
                                uName + "的播放列表" + playListName + "移除了曲目" + music.Value,
                                uName + "的播放列表" + playListName + "移除了曲目" + music.Value,
                                "UserInfo." + id + ".Music");
                        },
                        music =>
                        {
                            DiffDetected?.Invoke(
                                "http://music.163.com/#/playlist?id=" + id,
                                uName + "的播放列表" + playListName + "增加了曲目" + music.Value,
                                uName + "的播放列表" + playListName + "增加了曲目" + music.Value,
                                "UserInfo." + id + ".Music");
                        }
                        );
                }
            }
            col.Update(data);
            if (firstRun)
            {
                DiffDetected = tmpData;
                firstRun = false;
            }
        }

        public Action<string, string, string, string> DiffDetected { get; set; }
    }


}
