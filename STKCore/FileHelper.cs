using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject
{
    public static class FileHelper
    {
        /// <summary>
        /// 自动创建指定的目录,如果路径相关的目录不存在
        /// </summary>
        /// <param name="pathValue">目录</param>
        public static void ResolvePath(string pathValue)
        {
            string path = ".";
            foreach (var subStr in (pathValue).Split('/'))
            {
                path = path + "/" + subStr;
                if (subStr.Contains('.')) continue;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
        }
    }
}
