using System;
using OpenRA.Traits;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OpenRA.Mods.Common.AI.Esu.Database;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public class AsyncUnitDamageInformationLogger
    {
        private const int ThreadWaitBetweenLogCheckMillis = 100;

        private readonly Queue<UnitDamageData> LogQueue = new Queue<UnitDamageData>();
        private readonly object LogQueueLock = new object();
        private volatile bool IsShutdown;

        private readonly UnitDamageDataTable UnitDamageDataTable;

        public AsyncUnitDamageInformationLogger()
        {
            this.UnitDamageDataTable = new UnitDamageDataTable();
            StartThread();
        }

        private void StartThread()
        {
            IsShutdown = false;
            var thread = new Thread(() => Run());
            thread.Start();
        }

        private void Run()
        {
            while (!IsShutdown)
            {
                LogQueuedDamageInfo();
                if (IsShutdown) {
                    return;
                }

                try {
                    Thread.Sleep(ThreadWaitBetweenLogCheckMillis);
                } catch (ThreadInterruptedException) {
                    /* Continue. */
                }
            }
        }

        private void LogQueuedDamageInfo()
        {
            while (!IsShutdown)
            {
                UnitDamageData info;
                lock (LogQueueLock)
                {
                    // Return when there is nothing left in the queue.
                    if (LogQueue.Count == 0)
                    {
                        return;
                    }
                    info = LogQueue.Dequeue();
                }

                if (info == null)
                {
                    return;
                }

                UnitDamageDataTable.InsertUnitDamageData(info);
            }
        }

        public void QueueUnitDamageData(UnitDamageData data)
        {
            if (data.Damage <= 0) {
                return;
            }

            lock (LogQueueLock)
            {
                LogQueue.Enqueue(data);
            }
        }

        public void Shutdown()
        {
            IsShutdown = true;
        }
    }
}
