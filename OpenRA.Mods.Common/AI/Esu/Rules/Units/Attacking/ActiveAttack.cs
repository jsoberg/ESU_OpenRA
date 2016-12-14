using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        /** Stack holding most recent target position to oldest target position. */
        public readonly Stack<CPos> TargetPositionStack;

        public List<Actor> AttackTroops;

        public ActiveAttack(CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            this.TargetPositionStack = new Stack<CPos>();
            TargetPositionStack.Push(targetPosition);

            this.AttackTroops = new List<Actor>(attackTroops);
        }
    }
}
