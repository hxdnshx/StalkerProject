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

        public void Start()
        {
            Prepare();
            isCancel = new CancellationTokenSource();
            updateJob = new Task(() => { UpdateLoop(isCancel.Token); }, isCancel.Token);
            updateJob.Start();
        }

        private void UpdateLoop(CancellationToken token)
        {
            for (;;)
            {
                Run();
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
