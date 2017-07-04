using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject
{
    public class DataHelper
    {
        public static void IncrementalLoop<T,U>(
            ref int pos,ref bool isFoundHead,IList<U> newList,Func<U,T> transFunc
            ,Func<T,T,bool> compareFunc,IList<T> oldList,Action<T> onRemove,Action<T> onInsert)
        {
            List<T> pendingInsert = new List<T>();
            foreach (var newElement in newList)
            {
                T ele = transFunc(newElement);
                int index = oldList.FindIndex(item => compareFunc(item,ele));
                StepInfo refsi = dream.Steps.ElementAtOrDefault(index);
                string sid = refsi?.Status["sid"];
                if (index == -1)
                {
                    if (isFoundHead)
                        throw new Exception("莫名的顺序,怀疑不对");
                    si = new StepInfo
                    {
                        Status = new Dictionary<string, string>(),
                        Images = new List<string>(),
                        Comments = new List<CommentInfo>()
                    };
                    pendingInsert.Add(si);
                    foreach (var val in (JObject)stepInfo)
                    {
                        if (val.Key == "images")
                        {
                            foreach (var imgPath in (JArray)val.Value)
                            {
                                si.Images.Add(imgPath["path"].Value<string>());
                            }
                        }
                        else
                            si.Status[val.Key] = val.Value.Value<string>();
                    }
                }
                else
                {
                    isFoundHead = true;
                    if (pos - index < 0)
                        throw new Exception("???WTF???");
                    for (int j = index + 1; j <= pos; j++)
                    {
                        if (!dream.Steps[j].isRemoved)
                        {
                            dream.Steps[j].isRemoved = true;
                            unresolvedDiff++;
                            DiffDetected?.Invoke(
                                "http://nian.so/m/dream/" + dream.Status["id"],
                                uName + "删除了一条在" + title + "的足迹",
                                "足迹内容:" + dream.Steps[j].Status["content"],
                                "DreamList." + id + "." + dream.Steps[j].Status["sid"]);
                        }
                    }
                    pos = index - 1;
                    unresolvedComment =
                        int.Parse(refsi.Status["comments"]) -
                        ((JObject)stepInfo)["comments"].Value<int>();
                    refsi.Status["comments"] = ((JObject)stepInfo)["comments"].Value<string>();
                    si = refsi;
                }
            }
            
        }
    }
}
