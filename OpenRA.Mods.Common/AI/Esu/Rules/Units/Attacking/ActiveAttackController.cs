using OpenRA.Mods.Common.AI.Esu.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttackController : INotifyDamage
    {
        private readonly List<ActiveAttack> CurrentAttacks;

        public ActiveAttackController()
        {
            this.CurrentAttacks = new List<ActiveAttack>();

            // Add to callback list to get damage callbacks.
            DamageNotifier.AddDamageNotificationListener(this);
        }

        public IEnumerable<ActiveAttack> GetActiveAttacks()
        {
            return CurrentAttacks;
        }

        public void AddNewActiveAttack(Queue<Order> orders, CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            CurrentAttacks.Add(new ActiveAttack(targetPosition, attackTroops));
            AddAttackMoveOrders(orders, targetPosition, attackTroops);
        }

        private void AddAttackMoveOrders(Queue<Order> orders, CPos targetPosition, IEnumerable<Actor> attackActors)
        {
            foreach (Actor actor in attackActors)
            {
                var move = new Order("AttackMove", actor, false) { TargetLocation = targetPosition };
                orders.Enqueue(move);
            }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            // TODO Stub
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
