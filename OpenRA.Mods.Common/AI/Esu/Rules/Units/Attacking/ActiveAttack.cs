using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        public List<Actor> AttackTroops;

        /** Stack holding most recent target position to oldest target position. */
        public readonly Stack<CPos> TargetPositionStack;

        /** Contains the collection of positions that this attack was damaged from.*/
        private readonly List<CPos> AttackerLocationList;
        private int LastTickDamageTaken;

        public ActiveAttack(CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            this.TargetPositionStack = new Stack<CPos>();
            TargetPositionStack.Push(targetPosition);

            this.AttackerLocationList = new List<CPos>();
            this.AttackTroops = new List<Actor>(attackTroops);
        }

        public void AttackedFrom(Actor attacker, World world)
        {
            AttackerLocationList.Add(attacker.Location);
            LastTickDamageTaken = world.GetCurrentLocalTickCount();
        }
    }
}
