﻿using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Geometry;

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

        public void IssueNextAttack(StrategicWorldState state, Queue<Order> orders)
        {
            CPos nextMove = GeometryUtils.Center(AttackerLocationList);
            if (nextMove == CPos.Invalid) {
                // TODO try to get a location from scout reports or somewhere else.
                return;
            }

            TargetPositionReachedTickCount = 0;
            AddAttackMoveOrders(orders, nextMove, AttackTroops);
        }

        public void AddAttackMoveOrders(Queue<Order> orders, CPos targetPosition, IEnumerable<Actor> attackActors)
        {
            foreach (Actor actor in attackActors)
            {
                var move = new Order("AttackMove", actor, false) { TargetLocation = targetPosition };
                orders.Enqueue(move);
            }
        }
    }
}
