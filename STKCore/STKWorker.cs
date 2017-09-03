using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StalkerProject
{
    public class STKWorker : ISTKService
    {
        private Task updateJob;
        private CancellationTokenSource isCancel;
        public int Interval { get; set; }
        public string Alias { get; set; }
        /// <summary>
        /// 是否在进行单元测试,单元测试时会去除一些延时要素
        /// </summary>
        public bool IsTest { get; set; }
        protected bool IsFirstRun { get; private set; }
        public Exception ThrowedException = null;

        public bool ServiceStatus => updateJob?.Status == TaskStatus.Running;
        public bool IsWaitingForNextRound { get; private set; }

        public void Start()
        {
            Prepare();
            isCancel = new CancellationTokenSource();
            updateJob = new Task(() => { UpdateLoop(isCancel.Token); }, isCancel.Token);
            updateJob.Start();
            IsFirstRun = true;
        }

        private void UpdateLoop(CancellationToken token)
        {
            for (;;)
            {
                try {
                    IsWaitingForNextRound = false;
                    Run();
                }
                catch (Exception e) {
                    ThrowedException = e;
                    Console.WriteLine($"STKWorker {Alias}: An unhandled exception occured.");
                    Console.WriteLine(e);
                    Terminate();
                }
                IsWaitingForNextRound = true;
                IsFirstRun = false;
                token.WaitHandle.WaitOne(Interval);
                token.ThrowIfCancellationRequested();
            }
        }

        protected virtual void Run()
        {
            
        }

        protected virtual void Prepare()
        {
            
        }

        protected void Terminate() {
            isCancel.Cancel();
        }

        public void Stop()
        {
            if (updateJob.IsCompleted)
            {
                isCancel.Dispose();
                return;
            }
            isCancel.Cancel();
            try
            {
                updateJob.Wait();
            }
            catch (AggregateException e)
            {
                foreach (var v in e.InnerExceptions)
                    Console.WriteLine(e.Message + " " + v.Message);
            }
            finally
            {
                isCancel.Dispose();
            }
        }

        public virtual void LoadDefaultSetting()
        {
            int randInt = new Random().Next(1, 100000);
            Alias = "ServerChan" + randInt.ToString();
            Interval = 3600000;
        }
    }
}
