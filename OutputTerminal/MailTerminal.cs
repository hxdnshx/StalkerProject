using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Net.Mail;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using SQLite.Net;

namespace StalkerProject.OutputTerminal
{
    public class MailTerminal : STKWorker
    {
        private SQLiteConnection database;
        private DateTime LastCheckTime;
        private string checkFile => Alias + ".shortcut";
        public int MailPort { get; set; }
        public string MailHost { get; set; }
        public string MailUName { get; set; }
        public string MailPWord { get; set; }
        public bool MailSSL { get; set; }
        public bool StartTLS { get; set; }
        public string MailSender { get; set; }
        public string MailTarget { get; set; }

        protected override void Prepare() {
            base.Prepare();
            if (File.Exists(checkFile))
                LastCheckTime = DateTime.Parse(File.ReadAllText(checkFile));
        }

        protected override void Run() {
            base.Run();
            if (IsFirstRun && database==null) {
                Thread.Sleep(10000);
                if (database == null)
                {
                    Console.WriteLine("No DiffDatabase connected,Service Terminate");
                    Terminate();
                    return;
                }
            }
            var list = (from p in database.Table<DiffData>()
                        where p.OutputTime > LastCheckTime
                        orderby p.OutputTime descending
                        select p);
            string AllOutput = "以下是新更新的内容：";
            foreach (var outputData in list)
            {
                AllOutput += string.Format(
                    "\n\n{0}\n{1}\n相关链接:{2}", outputData.Summary, outputData.Content, outputData.RelatedAddress);
            }
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("Stalker Project", MailSender));
            msg.To.Add(new MailboxAddress(MailTarget));
            msg.Subject = "日常报告 - " + DateTime.Now;
            msg.Body = new TextPart("plain")
            {
                Text = AllOutput
            };
            Console.WriteLine("Start Send Mail");
            Console.WriteLine(AllOutput);
            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                if (StartTLS)
                    client.Connect(MailHost, MailPort, SecureSocketOptions.StartTls);
                else
                    client.Connect(MailHost, MailPort, MailSSL);
                Console.WriteLine("Connected Mail Host:" + MailHost);
                client.AuthenticationMechanisms.Remove("XOUTH2");
                Console.WriteLine("Auth:" + MailUName);
                client.Authenticate(MailUName, MailPWord);
                Console.WriteLine("Auth success,Sending Mail...");
                client.Send(msg);
                client.Disconnect(true);
            }
            Console.WriteLine("Mail Sent");
            File.WriteAllText(checkFile, DateTime.Now.ToString());
            LastCheckTime = DateTime.Now;
        }

        private void UpdateLoop(CancellationToken token)
        {
            token.WaitHandle.WaitOne(10000);
            
            for (;;)
            {
                
                token.WaitHandle.WaitOne(Math.Max(Interval, 3600000));
                token.ThrowIfCancellationRequested();
            }
        }

        public override void LoadDefaultSetting()
        {
            base.LoadDefaultSetting();
            int randInt=new Random().Next(1,100000);
            Alias="MailTerminal" + randInt.ToString();
            LastCheckTime = DateTime.Now;
            MailHost = "smtp.qq.com";
            MailPort = 465;
            MailSSL = true;
            Interval = 3600000;//一天一次
        }

        public void GetDatabase(SQLiteConnection db)
        {
            database = db;
        }
    }
}
