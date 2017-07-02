using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StalkerProject
{
    public class ServiceManager
    {
        public List<ISTKService> ActiveServices { get; set; }
        public Dictionary<string,Type> ServiceTypes=new Dictionary<string, Type>();
        private Dictionary<int,Type> _actionTypes=new Dictionary<int, Type>();

        public static string GetExePath()
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            FileInfo fi=new FileInfo(proc.MainModule.FileName);
            return fi.DirectoryName;
        }

        /// <summary>
        /// 因为Action<>根据不同的泛型参数个数，对应的是不同的类型，所以需要获取一下w
        /// </summary>
        private void EnumActions()
        {
            _actionTypes[0] = typeof(Action);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.FullName.Contains("System.Action") && type.IsGenericTypeDefinition)
                    {
                        Console.WriteLine(type.ToString());
                        _actionTypes[type.GetGenericArguments().Length] = type;
                    }
                }
            }
        }

        public void LoadAssemblyInfo()
        {
            List<string> loadedAssemblyList=new List<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                loadedAssemblyList.Add(assembly.ManifestModule.Name);
                //loadedAssemblyList.Add(assembly.);
                foreach (var aType in assembly.GetTypes())
                {
                    if(aType.Name== "DomainProxy")
                        Console.Write("");
                    if (aType.GetInterfaces().Contains(typeof(ISTKService)))
                        ServiceTypes.Add(aType.Name,aType);
                }
            }
            var enumPath = GetExePath();
            var dllList = Directory.GetFiles(enumPath, "*.dll");
            foreach (var file in dllList)
            {
                FileInfo fi=new FileInfo(file);
                if (loadedAssemblyList.Contains(fi.Name)) continue;
                var assembly = Assembly.LoadFrom(file);
                foreach (var aType in assembly.GetTypes())
                {
                    if (aType.GetInterfaces().Contains(typeof(ISTKService)))
                        ServiceTypes.Add(aType.Name, aType);
                }
            }
        }

        public ServiceManager()
        {
            EnumActions();
            LoadAssemblyInfo();
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
                Type classType = null;
                ServiceTypes.TryGetValue(className, out classType);
                if (classType == null) throw new ArgumentException("不存在服务:" + className);
                ISTKService srv = (ISTKService) Activator.CreateInstance(classType);
                srv.LoadDefaultSetting();//Load Default Setting First
                foreach (var element in xElement.Elements())
                {
                    var propInfo=classType.GetProperty(element.Name.ToString());
                    if(propInfo==null)continue;
                    propInfo.SetValue(srv,
                        Convert.ChangeType(element.Value,propInfo.PropertyType));
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
                var fromMethodType = fromArgs.Length>0 ?
                    _actionTypes[fromArgs.Length].MakeGenericType(fromArgs)
                    :typeof(Action);
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

        public void SaveSetting(string destPath)
        {
            var doc=new XDocument();
            var root=new XElement("STKProject");
            doc.Add(root);
            {
                var srvRoot=new XElement("Services");
                root.Add(srvRoot);
                foreach (var service in ActiveServices)
                {
                    var serviceXml= new XElement(
                            "Service",
                            new XAttribute("Class", service.GetType().Name));
                    srvRoot.Add(serviceXml);
                    foreach (var property in service.GetType().GetProperties())
                    {
                        if (!property.PropertyType.IsPrimitive
                            && !(property.PropertyType==typeof(string))) continue;
                        var propName = property.Name;
                        var propValue = property.GetValue(service);
                        serviceXml.Add(new XElement(propName,propValue));
                    }
                }
            }
            {
                var connRoot=new XElement("Connections");
                root.Add(connRoot);
                foreach (var service in ActiveServices)
                {
                    foreach (var property in service.GetType().GetProperties())
                    {
                        var originType = property.PropertyType;
                        if (originType.IsConstructedGenericType) originType = originType.GetGenericTypeDefinition();
                        if (_actionTypes.FirstOrDefault(pair=>pair.Value==originType).Value==null) continue;
                        var value = property.GetValue(service) as Delegate;
                        if (value == null) continue;
                        foreach (var @delegate in value.GetInvocationList())
                        {
                            var src = @delegate.Target as ISTKService;
                            if (src == null) continue;
                            connRoot.Add(new XElement(
                                "Connection",
                                new XElement(
                                    "From",
                                    src.Alias + "." + @delegate.Method.Name),
                                new XElement(
                                    "To",
                                    service.Alias + "." + property.Name)));
                        }
                    }
                }
            }
            doc.Save(destPath);
        }
    }
}
