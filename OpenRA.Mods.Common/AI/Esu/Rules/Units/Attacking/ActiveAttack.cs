using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        public List<Actor> AttackTroops;
        public int LastTickDamageMade;

        private int TargetPositionReachedTickCount;

        /** Stack holding most recent target position to oldest target position. */
        private readonly Stack<CPos> TargetPositionStack;

        /** Contains the collection of positions that this attack was damaged from.*/
        private readonly List<CPos> AttackerLocationList;

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
            LastTickDamageMade = world.GetCurrentLocalTickCount();
        }

        public void AttackedTo(Actor attackingTroop, Actor actorAttacked, World world)
        {
            LastTickDamageMade = world.GetCurrentLocalTickCount();
        }

        public bool HasReachedTargetPosition(World world)
        {
            if (TargetPositionReachedTickCount > 0) {
                return true;
            }

            CPos targetPosition = TargetPositionStack.Peek();
            foreach (Actor troop in AttackTroops) {
                if (troop.Location == targetPosition) {
                    TargetPositionReachedTickCount = world.GetCurrentLocalTickCount();
                    return true;
                }
            }
            return false;
        }
    }
}
