using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StalkerProject;
using RedXuCSharpClass;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace StalkerProject.NianObserver
{
    internal static class NianApiAddr
    {
        /// <summary>
        /// GET api.nian.so/check/email
        ///	获取指定的邮箱是否被使用
        ///	参数:
        ///		email:邮箱地址
        ///	返回json：
        ///		{
        ///			"status" : "200",
        ///			"error" : "0",
        ///			"data" : "1",   #0为未注册，1为注册
        ///		}
        ///	*在这里返回后Set-Cookie了一个flash值，用途不明
        /// </summary>
        public static string CheckEMail = "api.nian.so/check/email";
        /// <summary>
        /// POST api.nian.so/user/login
        ///	登陆
        ///	参数:
        ///		email:地址
        ///		password:加密后的密码
        ///	返回json：
        ///		{
        ///			"status" : "200",
        ///			"error" : "0",
        ///			"data" :
        ///				{
        ///					"uid" : "111",		#用户编号
        ///					"shell" : "xxx",	#获得的授权编号
        ///					"name" : "Fin_"		#用户昵称
        ///				}
        ///		}
        /// </summary>
        public static string Login = "api.nian.so/user/login";
        /// <summary>
        /// GET api.nian.so/user/[uid]/dreams
        ///	获取一个用户的所有梦想（咸鱼）
        ///	参数:
        ///		uid:用户编号（与shell是唯一对应的）
        ///		shell:登陆获得的授权编号
        ///	返回json:
        ///		{
        ///			"status" : "200",
        ///			"error" : "0",
        ///			"data" :
        ///				{
        ///					"page" : "1",					#当前页
        ///					"perPage" : "20",				#每页数据总数
        ///					dreams : 
        ///					[
        ///						{
        ///							"id" : "3295188",		#梦想id
        ///							"title" : "Record",		#梦想名
        ///							"image" : "p4.png",		#梦想封面图
        ///							"percent" : "0",		#梦想是否完成？
        ///						},
        ///						#多个与上面相同的项目
        ///					]
        ///				}
        ///		}
        /// </summary>
        public static string GetDreams = "api.nian.so/user/{0}/dreams";
        /// <summary>
        /// GET api.nian.so/game/over
        ///	获取指定用户是否处于游戏结束状态
        ///	参数:
        ///		uid:用户编号（与shell是唯一对应的）
        ///		shell:登陆获得的授权编号
        ///	返回json:
        ///		{
        ///			"status" : "200",
        ///			"error" : "0",
        ///			"data" :
        ///				{
        ///					"gameover" : "0"	#0是未结束
        ///				}
        ///		}
        /// </summary>
        public static string IsGameOver = "api.nian.so/game/over";
        /// <summary>
        /// GET api.nian.so/user/[uid]
        ///	获取指定用户的信息
        ///	参数:
        ///		uid:用户编号（与shell是唯一对应的）
        ///		shell:登陆获得的授权编号
        ///	返回json:
        ///		{
        /// 		   "status" : 200,
        ///		    "error" : 0,
        ///		    "data" : 
        ///		    	{
        ///	        		"user" : 
        ///	        			{
        ///	        	    		"uid" : "938737",				#用户编号(...
        ///	        	    		"name" : "Fin_",				#用户名
        ///	        	    		"email" : "741782800@qq.com",	#邮箱
        ///	        	    		"lastdate" : "1497910848",		#上次更新日期？
        ///    	    	    		"coin" : "9",					#念币
        ///        		    		"vip" : "0",					#是否vip（那个充120的vip
        ///            				"follows" : "1",				#关注者
        ///            				"followed" : "1",				#粉丝
        ///            				"is_followed" : "1",			#自己是否关注了这个人
        ///            				"step" : "15",					#总足迹
        ///            				"dream" : "4",					#总梦想
        ///            				"level" : "0",					#等级
        ///            				"cover" : "",					#ios/安卓上的个人页面背景图
        ///            				"likes" : "0",					#被人点赞的次数？
        ///            				"topics_count" : "0",			#???
        ///            				"deadline" : "20:15",			#离游戏结束剩下的时间（搜索他人的信息时永远为0）
        ///            				"phone" : "0",					#电话信息
        ///            				"isban" : "0",					#是否被封禁
        ///            				"gender" : "0",					#性别（0是保密啦）
        ///            				"isMonthly" : "0",				#？？？
        ///            				"pet_count" : "0",				#ios/安卓上的宠物个数
        ///            				"member" : "0",					#？？
        ///            				"find_by_phone" : "1",			#可以根据电话号码找到
        ///            				"find_by_weibo" : "1",			#可以根据微博找到
        ///            				"balance" : "0"					#余额（根据念充的）
        ///        				}
        ///    			}
        ///		}
        /// </summary>
        public static string GetUserInfo = "api.nian.so/user/{0}";
        /// <summary>
        /// GET api.nian.so/letter/list
        ///	获取自己收到的信息（点赞，关注，私信）
        ///	参数:
        ///		uid:用户编号（与shell是唯一对应的）
        ///		shell:登陆获得的授权编号
        ///	返回json:
        ///	{
        ///	    "notice_reply" : "0",	#他人的评论
        ///	    "notice_like" : "0",	#他人的点赞
        ///	    "notice_news" : "0"		#？？？
        ///	}
        /// </summary>
        public static string GetLettersInfo = "api.nian.so/letter/list";
        /// <summary>
        /// GET api.nian.so/v2/explore/follow
        ///	按照自己的follow显示进展
        ///	参数:
        ///		page : 页数
        ///		uid : 用户编号（与shell是唯一对应的）
        ///		shell : 登陆获得的授权编号
        ///	返回json:
        ///	{
        ///    	"status" : 200,
        ///    	"error" : 0,
        ///    	"data" : 
        ///		    {
        ///		        "count" : "20",		#items总数
        ///		        "items" : 
        ///		        	[
        ///			            {
        ///			                "dream" : "3295188",							#所属梦想
        ///			                "sid" : "23075422",								#进展编号
        ///			                "uid" : "938737",								#所属用户编号
        ///			                "user" : "Fin_",								#所属用户名
        ///			                "content" : "",									#文本内容
        ///			                "lastdate" : "1498055962",						#发布时间
        ///			                "title" : "Record",								#所属梦想的标题
        ///			                "image" : "938737_14980559610.png",				#进展对应的图片
        ///			                "width" : "490",								#图片的宽度
        ///			                "height" : "746",								#图片的高度
        ///			                "likes" : "0",									#几个人点赞？
        ///			                "liked" : "0",
        ///			                "comments" : "0",								#几个人评论
        ///			                "member" : "0",									#？？？
        ///			                "images" : 										#多个图片
        ///			                	[
        ///				                    {
        ///				                        "path" : "938737_14980559610.png",	#在img.nian.so/step/[path]!large 获取
        ///				                        "width" : "490",
        ///				                        "height" : "746"
        ///				                    }
        ///			                	],
        ///			                "type" : "6"									#6对应只有图片，0对应只有文本，5对应有文本有图片...等等
        ///			            }
        ///		        	],
        ///		        "page" : "1"
        ///		    }
        ///	}
        /// </summary>
        public static string GetExplore = "api.nian.so/v2/explore/follow";
        /// <summary>
        /// GET nian.so/api/searchuser.php
        ///	寻找某个特定的用户
        ///	参数:
        ///		uid : 用户编号（与shell是唯一对应的）
        ///		shell : 登陆获得的授权编号
        ///		page : 页数
        ///		keyword : 关键词
        ///	返回json:
        ///		{
        ///		    "users": 
        ///			    [
        ///			        {
        ///			            "uid": "930238",			#用户id
        ///			            "user": "lotusLand",		#用户名
        ///			            "follow": "0"				#是否已follow
        ///			        }
        ///			    ]
        ///		}
        /// </summary>
        public static string SearchUser = "nian.so/api/searchuser.php";
        public static string GetDreamUpdate = "api.nian.so/v2/multidream/{0}";
        /// <summary>
        /// GET api.nian.so/step/[stepID]/comments
        ///	获取某个进展的所有评论
        ///	参数:
        ///		uid : 用户编号（与shell是唯一对应的）
        ///		shell : 登陆获得的授权编号
        ///		page : 页码
        ///	返回json:
        ///		{
        ///		    "status": 200,
        ///		    "error": 0,
        ///		    "data": {
        ///		        "step": 
        ///			        {
        ///			            "sid": "22865256",
        ///			            "content": "如果这就是我的命运的话",
        ///			            "uid": "930238",
        ///			            "user": "lotusLand",
        ///			            "image": "930238_1495992708.png",
        ///			            "lastdate": "1497073851",
        ///			            "width": "0",
        ///			            "height": "0",
        ///			            "likes": "0",
        ///			            "liked": "0",
        ///			            "comments": "2",
        ///			            "dream": "3234872",
        ///			            "hidden": "0"
        ///			        },
        ///		        "comments": 
        ///			        [
        ///			            {
        ///			                "id": "6536279",					#回应ID
        ///			                "content": "是为了创造新的故事",	#回应内容
        ///			                "uid": "930238",					#回应用户id
        ///			                "user": "lotusLand",				#回应用户名
        ///			                "lastdate": "1497074102",			#回应日期
        ///			                "type": "0"
        ///			            }
        ///			        ],
        ///		        "page": "1",
        ///		        "perPage": "15"
        ///		    }
        ///		}
        /// </summary>
        public static string GetStepComments = "api.nian.so/step/{0}/comments";

    }
    public class NianApi
    {
        private HttpHelper helper;
        private string uid;
        private string shell;

        public NianApi()
        {
            helper=new HttpHelper();
            helper.SetUserAgent("NianiOS/5.0.3 (iPad; iOS 10.3.1; Scale/2.00)");
            helper.SetAccept("*/*");
            helper.SetEncoding(Encoding.UTF8);
        }

        public void GetLoginToken(out string userId, out string shellToken)
        {
            userId = uid;
            shellToken = shell;
        }

        private NameValueCollection ShellParameter()
        {
            var parameters= HttpUtility.ParseQueryString(string.Empty);
            parameters["uid"] = uid;
            parameters["shell"] = shell;
            return parameters;
        }

        private string Request(string uri, bool isAuth, string[] additionalParams=null)
        {
            var uriBuilder = new UriBuilder(uri);
            var parameters = isAuth ? ShellParameter() : new NameValueCollection();
            if (additionalParams != null)
            {
                if (additionalParams.Length % 2 != 0)
                    throw new ArgumentException("Invalid additionParams");
                int i = 0;
                for (i = 0; i < additionalParams.Length / 2; i++)
                    parameters[additionalParams[i * 2]] = additionalParams[i * 2 + 1];
            }
            uriBuilder.Query = parameters.ToString();
            return helper.HttpGet(uriBuilder.ToString());
        }

        public bool RestoreLogin(string userId, string shellToken)
        {
            uid = userId;
            shell = shellToken;

            var ret = Request(NianApiAddr.IsGameOver, true);
            var jsonDoc = JObject.Parse(ret);
            if (jsonDoc["status"].Value<string>() == "200")
                return true;
            return false;
        }

        public bool Login(string mailAddr, string passWord)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            passWord = "n*A" + passWord;//Add Salt
            var data= System.Text.Encoding.ASCII.GetBytes(passWord);
            var result = md5.ComputeHash(data);
            string byte2String = null;

            for (int i = 0; i < result.Length; i++)
            {
                byte2String += result[i].ToString("x2");
            }
            {
                string ret = Request(NianApiAddr.CheckEMail,false,
                    new []
                    {
                        "email",mailAddr
                    });
                var jsonDoc = JObject.Parse(ret);
            }
            {
                var uriBuilder = new UriBuilder(NianApiAddr.Login);
                var parameters = HttpUtility.ParseQueryString(string.Empty);
                parameters["email"] = mailAddr;
                parameters["password"] = byte2String;
                string ret = helper.HttpPost(uriBuilder.ToString(), parameters.ToString());
                var jsonDoc = JObject.Parse(ret);
                if (jsonDoc["status"].Value<string>() == "200")
                {
                    uid = jsonDoc["data"]["uid"].Value<string>();
                    shell = jsonDoc["data"]["shell"].Value<string>();
                    return true;
                }
                return false;
            }
        }

        public JObject GetUserData(string userId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                userId = uid;
            var ret = Request(string.Format(NianApiAddr.GetUserInfo, userId), true);
            var jsonDoc = JObject.Parse(ret);
            if (jsonDoc["status"].Value<string>() == "200")
            {
                return jsonDoc["data"] as JObject;
            }
            return null;
        }

        public JObject GetUserDreams(string userId = null,int page=1)
        {
            if (string.IsNullOrWhiteSpace(userId))
                userId = uid;
            var ret = Request(string.Format(NianApiAddr.GetDreams, userId), true,
                new [] {"page",page.ToString()});
            var jsonDoc = JObject.Parse(ret);
            if (jsonDoc["status"].Value<string>() == "200")
            {
                return jsonDoc["data"] as JObject;
            }
            return null;
        }

        public JObject GetDreamUpdate(string dreamId, int page = 1)
        {
            var ret = Request(string.Format(NianApiAddr.GetDreamUpdate, dreamId), true,
                new[] {"page", page.ToString()});
            var jsonDoc = JObject.Parse(ret);
            if (jsonDoc["status"].Value<string>() == "200")
            {
                return jsonDoc["data"] as JObject;
            }
            return null;
        }
    }
}
