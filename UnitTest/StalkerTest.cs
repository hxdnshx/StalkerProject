using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalkerProject.BilibiliObserver;
using System.IO;
using System.Xml.Linq;
using LiteDB;
using StalkerProject.NianObserver;
using SQLite.Net;

namespace StalkerProject.UnitTest
{
    [TestClass]
    public class STKTest
    {
        [TestMethod]
        public void BilibiliTest() {
            BilibiliObserver.LiveStalker live = new LiveStalker();
            live.TargetRoom = 5269;//iPanda 7*24 channel
            AutoResetEvent isFetched = new AutoResetEvent(false);
            live.DiffDetected += (string a, string b, string c, string d) => {
                isFetched.Set();
            };
            live.Start();
            if (!isFetched.WaitOne(4000)) {
                Assert.Fail();
                return;
            }
        }

        [TestMethod]
        public void NeteaseFetchTest() {
            Assert.IsTrue(System.IO.File.Exists("netease.js"));
            var fetch = new NetEaseObserver.NetEaseFetch();
            fetch.TargetUser = "viviking";//普通的某个用户
            fetch.IsTest = true;
            AutoResetEvent isFetched = new AutoResetEvent(false);
            fetch.OnDataFetched += obj => {
                isFetched.Set();
            };
            fetch.Start();
            if (!isFetched.WaitOne(120000)) {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void CasperJSConfigTest() {
            ProcessStartInfo psi = new ProcessStartInfo("casperjs")
            {
                RedirectStandardOutput = false,
                UseShellExecute = false
            };
            Process fetchProc = Process.Start(psi);
            fetchProc.WaitForExit();
            Assert.AreEqual(fetchProc.ExitCode,0);
        }

        [TestMethod]
        public void NianTest() {
            if(File.Exists("NianTest.db"))
                File.Delete("NianTest.db");
            Assert.IsTrue(File.Exists("AccountData.xml"),"AccountData.xml Not Found");
            XDocument doc = XDocument.Load("AccountData.xml");
            var root = doc?.Element("AccountData")?.Element("NianTest");
            Assert.IsNotNull(root,"No NianTest Node");
            Assert.IsNotNull(root.Element("UserName"),"UserName not found");
            Assert.IsNotNull(root.Element("PassWord"),"PassWord Not Found");
            AutoResetEvent isFetched = new AutoResetEvent(false);

            //Init Database
            {
                var db = new LiteDatabase("NianTest.db");
                var col = db.GetCollection<NianData>();
                var data = new NianData();
                data.ListItems = new Dictionary<string, string>();
                data.Dreams = new List<DreamInfo>();
                col.Insert(data);
            }

            var nian = new NianObserver.NianStalker() {
                Alias = "NianTest",
                DiffDetected = (string a, string b, string c, string d) => {
                    isFetched.Set();
                },
                IsTest = true,
                UserName = root.Element("UserName").Value,
                PassWord = root.Element("PassWord").Value,
                TargetUID = 777777 //普通的一个用户(明明有神id却不用念了xxxx)
            };
            nian.Start();
            if (!isFetched.WaitOne(10000))
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void MailTest() {
            if (File.Exists("MailTest.db")) 
                File.Delete("MailTest.db");
            if(File.Exists("MailTerminal.shortcut"))
                File.Delete("MailTerminal.shortcut");
            Assert.IsTrue(File.Exists("AccountData.xml"), "AccountData.xml Not Found");
            XDocument doc = XDocument.Load("AccountData.xml");
            var root = doc?.Element("AccountData")?.Element("MailTest");
            Assert.IsNotNull(root, "No MailTest Node");
            var mail = new OutputTerminal.MailTerminal() {
                MailHost = root.Element("MailHost").Value,
                MailPort = int.Parse(root.Element("MailPort").Value),
                MailUName = root.Element("MailUName").Value,
                MailPWord = root.Element("MailPWord").Value,
                MailSender = root.Element("MailSender").Value,
                MailTarget = root.Element("MailTarget").Value,
                MailSSL = root.Element("MailSSL")?.Value == "true",
                Alias = "MailTerminal",
                Interval = 60000
            };
            var diffdb = new OutputTerminal.DiffDatabase() {
                Alias = "MailTest",
                DatabaseSource = src => {mail.GetDatabase(src);}
            };
            diffdb.Start();
            diffdb.InputData("http://Saigetsu.moe","测试","测试","MailTest");
            mail.Start();
            Thread.Sleep(5000);
            Assert.IsTrue(mail.IsWaitingForNextRound);
            Assert.IsTrue(mail.ServiceStatus);
        }
    }
}
