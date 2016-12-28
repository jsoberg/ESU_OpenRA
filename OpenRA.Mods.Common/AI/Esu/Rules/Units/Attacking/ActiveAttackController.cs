using OpenRA.Mods.Common.AI.Esu.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttackController : INotifyDamage
    {
        /** Number of ticks to wait before moving attack. */
        private static int TICKS_UNTIL_ATTACK_MOVE = 1000;

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
            ActiveAttack attack = new ActiveAttack(targetPosition, attackTroops);
            CurrentAttacks.Add(attack);
            attack.AddAttackMoveOrders(orders, targetPosition, attackTroops);
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
                } else if (self == e.Attacker) {
                    attack.AttackedTo(e.Attacker, self, World);
                    return true;
                }
            }
            return false;
        }

        public bool IsActorInvolvedInActiveAttack(Actor actor)
        {
            foreach (ActiveAttack attack in CurrentAttacks)
            {
                foreach (Actor troop in attack.AttackTroops)
                {
                    if (troop == actor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            RemoveDeadTroopsFromAttacks();
            MaintainActiveAttacks(state, orders);
        }

        private void RemoveDeadTroopsFromAttacks()
        {
            for (int i = CurrentAttacks.Count - 1; i >= 0; i--)
            {
                bool IsAttackOver = TrimAttack(CurrentAttacks[i]);
                if (IsAttackOver)
                {
                    CurrentAttacks.RemoveAt(i);
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

        private void MaintainActiveAttacks(StrategicWorldState state, Queue<Order> orders)
        {
            foreach (ActiveAttack attack in CurrentAttacks)
            {
                if (attack.HasReachedTargetPosition(state.World)
                    && (state.World.GetCurrentLocalTickCount() - attack.LastTickDamageMade) >= TICKS_UNTIL_ATTACK_MOVE)
                {
                    attack.IssueNextAttack(state, orders);
                }
            }
        }
    }
}
