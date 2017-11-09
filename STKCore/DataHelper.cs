using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject
{
    public interface IRemoveFlag
    {
        bool IsRemoved { get; set; }
    }

    public static class DateHelper
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

    public class DataHelper
    {
        /// <summary>
        /// 增量对比打包成函数的版本
        /// </summary>
        /// <typeparam name="T">目标列表的类型</typeparam>
        /// <typeparam name="U">新数据列表的类型</typeparam>
        /// <param name="pos">当前列表位置，一般是目标列表最后一个元素,默认时为-1</param>
        /// <param name="isFoundHead">是否已经找到匹配</param>
        /// <param name="newList">新列表</param>
        /// <param name="oldList">老列表</param>
        /// <param name="transFunc">数据类型转换函数</param>
        /// <param name="compareFunc">对比函数</param>
        /// <param name="onRemove">当检测出有元素被删除时</param>
        /// <param name="onInsert">当检测出有元素增加时（增加操作由函数内处理）</param>
        public static void IncrementalLoop<T,U>(ref int pos, ref bool isFoundHead, IEnumerable<U> newList, List<T> oldList, Func<U, T> transFunc, Func<T, T, bool> compareFunc, Action<T> onRemove, Action<T> onInsert) where T : class,IRemoveFlag
        {
            if (pos == -1)
                pos = oldList.FindLastIndex(info => info.IsRemoved == false);
            List<T> pendingInsert = new List<T>();
            foreach (var newElement in newList)
            {
                T ele = transFunc(newElement);
                int index = oldList.FindIndex(item => compareFunc(item,ele));
                if (index == -1)
                {
                    if (isFoundHead)
                    {
                        Console.WriteLine("发现了异常数据：" + ele.ToString() + "原位置数据：" + oldList[pos>=0?pos:0]);
                        oldList.Insert(pos+1,ele);
                        onInsert?.Invoke(ele);
                    }
                    else
                        pendingInsert.Add(ele);
                }
                else
                {
                    isFoundHead = true;
                    if (pos - index < 0)
                        throw new Exception("???WTF???");
                    for (int j = index + 1; j <= pos; j++)
                    {
                        if (!oldList[j].IsRemoved)
                        {
                            oldList[j].IsRemoved = true;
                            onRemove?.Invoke(oldList[j]);
                        }
                    }
                    pos = index - 1;
                }
            }
            for (int j = pendingInsert.Count - 1; j >= 0; j--)
            {
                oldList.Add(pendingInsert[j]);
                onInsert?.Invoke(pendingInsert[j]);
            }
        }
    }
}
