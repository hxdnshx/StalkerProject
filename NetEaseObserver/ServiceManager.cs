using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StalkerProject.NetEaseObserver;

namespace StalkerProject.NetEaseObserver
{
    class ServiceManager
    {
        public List<ISTKService> ActiveServices { get; set; }

        public ServiceManager()
        {
            ActiveServices=new List<ISTKService>();
        }
        /// <summary>
        /// 读取配置Xml文档
        /// 格式类似下面的：
        /// <STKProject>
        ///     <Services>
        ///         <Service Class="NetEaseFetch">
        ///             <!--Settings Of NetEaseFetch Service-->
        ///         </Service>
        ///     </Services>
        ///     <Connections>
        ///         <Connection>
        ///             <From>Alias.Method</From>
        ///             <To>Alias.Port</To>
        ///         </Connection>
        ///     </Connections>
        /// </STKProject>
        /// </summary>
        /// <param name="xmlPath"></param>
        public void ReadSetting(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            var root = doc.Element("STKProject");
            if (root == null) return;
            if (root.Element("Services") == null) return;
            if (root.Element("Connections") == null) return;
            foreach (var xElement in root.Element("Services").Elements("Service"))
            {
                string className = xElement.Attribute("Class")?.Value;
                if(className==null)continue;
                var classType = Type.GetType(className);
                if (classType == null) continue;
                ISTKService srv = (ISTKService) Activator.CreateInstance(classType);
                foreach (var element in xElement.Elements())
                {
                    var propInfo=classType.GetProperty(element.Name.ToString());
                    if(propInfo==null)continue;
                    propInfo.SetValue(srv,element.Value);
                }
                ActiveServices.Add(srv);
            }
            foreach (var xElement in root.Element("Connections").Elements("Connection"))
            {
                var from = xElement.Element("From").Value.Split('.');
                var to = xElement.Element("To").Value.Split('.');
                string fromAlias = from[0];
                string fromMethod = from[1];
                string toAlias = to[0];
                string toPort = to[1];
                ISTKService fromSrv;
                ISTKService toSrv;
                if((fromSrv=ActiveServices.FirstOrDefault(srv=>srv.Alias==fromAlias))==null)continue;
                if((toSrv = ActiveServices.FirstOrDefault(srv => srv.Alias == toAlias)) == null) continue;
                var fromType = fromSrv.GetType();
                var toType = toSrv.GetType();
                var fromMethodInfo = fromType.GetMethod(fromMethod);
                var toPortInfo = toType.GetProperty(toPort);
                var fromArgs = fromMethodInfo.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
                //https://stackoverflow.com/questions/12131301/how-can-i-dynamically-create-an-actiont-at-runtime
                //动态创建类型
                var fromMethodType = typeof(Action<>).MakeGenericType(fromArgs);
                if (toPortInfo.PropertyType != fromMethodType)
                {
                    Console.WriteLine(toPortInfo.PropertyType.ToString() + fromType + "is Not Equal");
                    continue;
                }
                var fromMethodDelegate = Delegate.CreateDelegate(fromMethodType, fromSrv, fromMethodInfo, true);
                var toPortValue = (Delegate)toPortInfo.GetValue(toSrv);
                var finalDelegate = Delegate.Combine(fromMethodDelegate, toPortValue);
                toPortInfo.SetValue(toSrv,finalDelegate);
                //https://stackoverflow.com/questions/3016429/reflection-and-operator-overloads-in-c-sharp
                //获取运算符重载

                //var toPortBindFunc = fromMethodType.GetMethod("op_AdditionAssignment");
                //toPortBindFunc.Invoke(toPortInfo, new Object[]{fromMethodDelegate});
            }
        }
    }
}
