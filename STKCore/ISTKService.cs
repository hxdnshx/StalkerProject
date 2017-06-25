using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalkerProject
{
    public class STKDescription : Attribute
    {
        public STKDescription()
        {
            Text = "";
        }

        public STKDescription(string txt)
        {
            Text = txt;
        }
        public string Text { get; set; }
    }

    public class STKInputPort : Attribute
    {
        
    }
    /// <summary>
    /// 对于属于ISTKService的类，会自动扫描类型为Action<>的属性，作为输出路径
    /// 并扫描加了STKInputPort Attibute的方法，作为输入路径，作为可以连接的点显示。
    /// 所有public的属性也会公开出来。
    /// </summary>
    public interface ISTKService
    {
        /// <summary>
        /// 用于标识这个服务的别名
        /// </summary>
        string Alias { get; set; }
        void Start();
        void Stop();
        /// <summary>
        /// 调用后加载对这个服务而言的默认配置
        /// </summary>
        void LoadDefaultSetting();

    }
}
