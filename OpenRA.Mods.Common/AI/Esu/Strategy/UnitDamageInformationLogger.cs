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
        private const int ThreadWaitBetweenLogCheckMillis = 10;
        private const int ChecksBeforeShuttingDownThread = 600;

        private readonly Queue<UnitDamageData> LogQueue = new Queue<UnitDamageData>();
        private readonly object LogQueueLock = new object();
        private volatile bool IsRunning = false;

        private readonly UnitDamageDataTable UnitDamageDataTable;

        public AsyncUnitDamageInformationLogger()
        {
            this.UnitDamageDataTable = new UnitDamageDataTable();
            StartThread();
        }

        private void StartThread()
        {
            var thread = new Thread(() => Run());
            thread.Start();
        }

        private void Run()
        {
            int numChecksWithoutLog = 0;

            IsRunning = true;
            while (IsRunning)
            {
                bool wasLogged = LogQueuedDamageInfo();
                // If we logged, try again immediately.
                if (wasLogged)
                {
                    numChecksWithoutLog = 0;
                    continue;
                }
                // If we didn't log, either shutdown the thread (if we haven't received a message in awhile) or sleep for a bit.
                else
                {
                    numChecksWithoutLog++;
                    if (numChecksWithoutLog >= ChecksBeforeShuttingDownThread)
                    {
                        numChecksWithoutLog = 0;
                        IsRunning = false;
                        return;
                    }
                    else
                    {
                        SleepThread();
                    }
                }
            }
        }

        /** @return true if something was logged, false otherwise. */
        private bool LogQueuedDamageInfo()
        {
            UnitDamageData info;
            lock (LogQueueLock)
            {
                // Return when there is nothing left in the queue.
                if (LogQueue.Count == 0) {
                    return false;
                }
                info = LogQueue.Dequeue();
            }

            if (info == null) {
                return false;
            }

            UnitDamageDataTable.InsertUnitDamageData(info);
            return true;
        }

        private void SleepThread()
        {
            try
            {
                Thread.Sleep(ThreadWaitBetweenLogCheckMillis);
            }
            catch (ThreadInterruptedException)
            {
                /* Continue. */
            }
        }

        public void QueueUnitDamageData(UnitDamageData data)
        {
            if (data.Damage <= 0) {
                return;
            }

            lock (LogQueueLock) {
                LogQueue.Enqueue(data);
            }

            if (!IsRunning) {
                StartThread();
            }
        }
    }
}
