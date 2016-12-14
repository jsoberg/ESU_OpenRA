using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class DamageNotifier : ITick, INotifyDamage
    {
        void ITick.Tick(Actor self)
        {
            // TODO Stub
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            // TODO Stub
        }
    }

    public sealed class DamageNotifierInfo : ITraitInfo
    {
        public object Create(ActorInitializer init)
        {
            return new DamageNotifier();
        }
    }
}
