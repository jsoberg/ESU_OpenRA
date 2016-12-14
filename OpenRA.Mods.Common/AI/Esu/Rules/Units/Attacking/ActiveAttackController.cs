using OpenRA.Mods.Common.AI.Esu.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttackController
    {
        private readonly List<ActiveAttack> CurrentAttacks;

        public ActiveAttackController()
        {
            this.CurrentAttacks = new List<ActiveAttack>();
        }

        public IEnumerable<ActiveAttack> GetActiveAttacks()
        {
            return CurrentAttacks;
        }

        public void AddNewActiveAttack(CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            CurrentAttacks.Add(new ActiveAttack(targetPosition, attackTroops));
        }

        public void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            RemoveDeadTroopsFromAttacks();
        }

        private void RemoveDeadTroopsFromAttacks()
        {
            foreach (ActiveAttack attack in CurrentAttacks)
            {
                bool IsAttackOver = TrimAttack(attack);
                if (IsAttackOver)
                {
                    CurrentAttacks.Remove(attack);
                }
            }
        }

        /** @return true if all troops in this attack are dead, false otherwise. */
        private bool TrimAttack(ActiveAttack attack)
        {
            foreach (Actor actor in attack.AttackTroops)
            { 
                if (actor.IsDead)
                {
                    attack.AttackTroops.Remove(actor);
                }
            }

            return (attack.AttackTroops.Count() == 0);
        }
    }
}
