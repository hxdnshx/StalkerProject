using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using SQLite.Net;

namespace StalkerProject.OutputTerminal
{
    public class FileTerminal : STKWorker,IDomainProxy
    {
        private SQLiteConnection database;
        private DateTime LastCheckTime;
        private string checkFile => Alias + ".shortcut";
        public string Path { get; set; }
        private FileServer m_fs;

        protected override void Prepare()
        {
            base.Prepare();
            m_fs = new FileServer();
            m_fs.SubUrl = Path;
            m_fs.WebDir = "./" + Path;
            if (File.Exists(checkFile))
                LastCheckTime = DateTime.Parse(File.ReadAllText(checkFile));
            if (Directory.Exists(m_fs.WebDir) == false)
            {
                Directory.CreateDirectory(m_fs.WebDir);
            }
        }

        protected override void Run()
        {
            base.Run();
            if (IsFirstRun && database == null)
            {
                waitToken.WaitHandle.WaitOne(10000);
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
            StringBuilder output = new StringBuilder();
            string AllOutput;
            output.Append("<html><head><meta content=\"text/html; charset=utf-8\" http-equiv=\"content-type\" /></head><body>");
            output.Append("<div>");
            output.Append("以下是新更新的内容：<br>");
            foreach (var outputData in list)
            {
                output.Append("<div>");
                output.Append(string.Format(
                    "\n\n{3}\n{0}\n{1}\n相关链接:<a href=\"{2}\">{2}</a>", 
                    outputData.Summary, 
                    outputData.Content, 
                    outputData.RelatedAddress,
                    outputData.OutputTime)
                    .Replace("\n","<br>"));
                output.Append("</div>");
            }
            output.Append("</div></body></html>");
            AllOutput = output.ToString();
            File.WriteAllText(checkFile, DateTime.Now.ToString());
            File.WriteAllText(
                FileServer.CombineDir(Path, DateTime.Now.ToString("yyyyMMddhhmmss") + ".html"),AllOutput);
            StringBuilder index = new StringBuilder();
            index.Append("<html><head><meta content=\"text/html; charset=utf-8\" http-equiv=\"content-type\" /></head><body>");
            foreach (var file in Directory.EnumerateFiles(Path))
            {
                FileInfo fi = new FileInfo(file);
                index.Append($"<a href=\"{FileServer.CombineDir(Path,fi.Name)}\">{fi.Name}</a><br>");
            }
            index.Append("</body></html>");
            File.WriteAllText(FileServer.CombineDir(Path,"index.html"),index.ToString());
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
            int randInt = new Random().Next(1, 100000);
            Alias = "FileTerminal" + randInt.ToString();
            Path = Alias;
            LastCheckTime = DateTime.Now;
            Interval = 3600000;//一天一次
        }

        public void GetDatabase(SQLiteConnection db)
        {
            database = db;
        }

        #region Implementation of IDomainProxy

        public bool OnHttpRequest(HttpListenerContext request)
        {
            if (m_fs == null) return false;
            return m_fs.OnHttpRequest(request);
        }

        #endregion
    }
}
