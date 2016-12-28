using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        private static readonly int DistanceFromStagedPosition = 10;

        public List<Actor> AttackTroops;
        public int LastTickDamageMade;

        private int TargetPositionReachedTickCount;
        private int StagedPositionReachedTickCount;
        public bool HasMovedFromStagedToTarget { get; internal set; }

        /** Position where the next attack should be staged before following through. */
        private CPos StagedPosition = CPos.Invalid;

        /** Stack holding most recent target position to oldest target position. */
        private readonly Stack<CPos> TargetPositionStack;

        /** Contains the collection of positions that this attack was damaged from.*/
        private readonly List<CPos> AttackerLocationList;

        public ActiveAttack(CPos targetPosition, CPos stagedPosition, IEnumerable<Actor> attackTroops)
        {
            this.TargetPositionStack = new Stack<CPos>();
            TargetPositionStack.Push(targetPosition);

            this.StagedPosition = stagedPosition;

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

        public bool HasReachedStagedPosition(World world)
        {
            if (StagedPositionReachedTickCount > 0 || StagedPosition == CPos.Invalid) {
                return true;
            }

            foreach (Actor troop in AttackTroops)
            {
                if (((StagedPosition.X - DistanceFromStagedPosition) < troop.Location.X && troop.Location.X < (StagedPosition.X + DistanceFromStagedPosition))
                        && ((StagedPosition.Y -DistanceFromStagedPosition) < troop.Location.Y && troop.Location.Y < (StagedPosition.Y + DistanceFromStagedPosition)))
                {
                    continue;
                } else {
                    return false;
                }
            }

            // All troops have reached staging area.
            StagedPositionReachedTickCount = world.GetCurrentLocalTickCount();
            return true;
        }

        public void MoveFromStagedToTarget(Queue<Order> orders)
        {
            if (!HasMovedFromStagedToTarget)
            {
                StagedPosition = CPos.Invalid;
                HasMovedFromStagedToTarget = true;
                AddAttackMoveOrders(orders, TargetPositionStack.Peek());
            }
        }

        public void MoveAttack(StrategicWorldState state, Queue<Order> orders)
        {
            CPos nextMove = GeometryUtils.Center(AttackerLocationList);
            if (nextMove == CPos.Invalid) {
                // TODO try to get a location from scout reports or somewhere else.
                return;
            }

            TargetPositionStack.Push(nextMove);
            TargetPositionReachedTickCount = 0;
            AddAttackMoveOrders(orders, nextMove);
        }

        public void AddAttackMoveOrders(Queue<Order> orders, CPos position)
        {
            foreach (Actor actor in AttackTroops)
            {
                var move = new Order("AttackMove", actor, false) { TargetLocation = position };
                orders.Enqueue(move);
            }
        }
    }
}
