﻿using System;
using OpenRA.Traits;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OpenRA.Mods.Common.AI.Esu.Database;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public class AsyncUnitDamageInformationLogger
    {
        private const int TicksUntilBatchLog = 400;

        private readonly Queue<UnitDamageData> DamageDataQueue;
        private readonly UnitDamageDataTable UnitDamageDataTable;
        private readonly object UnitDamageDataTableLock = new object();

        public AsyncUnitDamageInformationLogger()
        {
            this.DamageDataQueue = new Queue<UnitDamageData>();
            this.UnitDamageDataTable = new UnitDamageDataTable();
        }

        public void QueueUnitDamageData(UnitDamageData data)
        {
            if (data == null || data.Damage <= 0) {
                return;
            }

            DamageDataQueue.Enqueue(data);
        }

        public void Tick(World world)
        {
            if ((world.GetCurrentLocalTickCount() % TicksUntilBatchLog) == 0) {
                Queue<UnitDamageData> clone = DequeueAndCloneDamageDataQueue();
                ThreadPool.QueueUserWorkItem(t => LogQueuedDamageInfo(clone));
            }
        }

        private Queue<UnitDamageData> DequeueAndCloneDamageDataQueue()
        {
            Queue<UnitDamageData> clone = new Queue<UnitDamageData>();
            while (DamageDataQueue.Count > 0)
            {
                clone.Enqueue(DamageDataQueue.Dequeue());
            }
            return clone;
        }

        private void LogQueuedDamageInfo(Queue<UnitDamageData> damageDataQueue)
        {
            lock (UnitDamageDataTableLock)
            {
                using (SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection())
                {
                    if (connection == null) {
                        return;
                    }

                    using (SQLiteTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        foreach (UnitDamageData data in damageDataQueue)
                        {
                            UnitDamageDataTable.InsertUnitDamageData(connection, data);
                        }

                        transaction.Commit();
                    }
                    connection.Close();
                }
            }
        }
    }
}
