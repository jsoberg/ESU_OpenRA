using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        public CPos TargetPosition;
        public IEnumerable<Actor> AttackTroops;

        public ActiveAttack(CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            this.TargetPosition = targetPosition;
            this.AttackTroops = attackTroops;
        }
    }
}
