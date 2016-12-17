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

        private readonly World World;

        public ActiveAttackController(World world)
        {
            this.World = world;
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
            foreach (ActiveAttack attack in CurrentAttacks)
            {
                if (HandleDamage(attack, self, e))
                {
                    return;
                }
            }
        }

        /** @return true if this attack handled the damage, false otherwise. */
        private bool HandleDamage(ActiveAttack attack, Actor self, AttackInfo e)
        {
            foreach (Actor troop in attack.AttackTroops)
            {
                if (self == troop) {
                    attack.AttackedFrom(e.Attacker, World);
                    return true;
                }
            }
            return false;
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
            for (int i = attack.AttackTroops.Count - 1; i >= 0; i --)
            { 
                if (attack.AttackTroops[i].IsDead)
                {
                    attack.AttackTroops.RemoveAt(i);
                }
            }

            return (attack.AttackTroops.Count() == 0);
        }
    }
}
