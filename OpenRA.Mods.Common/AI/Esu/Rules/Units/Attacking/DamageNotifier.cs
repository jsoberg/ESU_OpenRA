using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class DamageNotifier
    {
        private static readonly List<INotifyDamage> DamageNotificationListeners = new List<INotifyDamage>();

        public static void AddDamageNotificationListener(INotifyDamage listener)
        {
            DamageNotificationListeners.Add(listener);
        }

        public static void Damaged(Actor self, AttackInfo e)
        {
            foreach (INotifyDamage listener in DamageNotificationListeners)
            {
                listener.Damaged(self, e);
            }
        }
    }
}
