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
        private readonly UnitDamageDataTable UnitDamageDataTable;
        private readonly object UnitDamageDataTableLock = new object();

        public AsyncUnitDamageInformationLogger()
        {
            this.UnitDamageDataTable = new UnitDamageDataTable();
        }

        public void QueueUnitDamageData(UnitDamageData data)
        {
            if (data == null || data.Damage <= 0) {
                return;
            }

            ThreadPool.QueueUserWorkItem(t => LogQueuedDamageInfo(data));
        }

        private void LogQueuedDamageInfo(UnitDamageData data)
        {
            lock (UnitDamageDataTableLock) {
                UnitDamageDataTable.InsertUnitDamageData(data);
            }
        }
    }
}
