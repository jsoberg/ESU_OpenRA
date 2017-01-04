using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        private const int DistanceToMoveAttack = 6;
        private const int DistanceFromStagedPosition = 6;

        public List<Actor> AttackTroops;
        public int LastTickDamageMade;

        public bool WasStagedPositionMoved { get; internal set; }
        private int TargetPositionReachedTickCount;
        private int StagedPositionReachedTickCount;
        public bool HasMovedFromStagedToTarget { get; internal set; }

        /** Position where the next attack should be staged before following through. */
        public CPos StagedPosition = CPos.Invalid;

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

        public void MoveStagedPosition(Queue<Order> orders, CPos newStagedPosition)
        {
            WasStagedPositionMoved = true;
            StagedPosition = newStagedPosition;
            StagedPositionReachedTickCount = 0;
            AddAttackMoveOrders(orders, StagedPosition);
        }

        public void MoveFromStagedToTarget(Queue<Order> orders)
        {
            if (!HasMovedFromStagedToTarget)
            {
                WasStagedPositionMoved = false;
                StagedPosition = CPos.Invalid;
                HasMovedFromStagedToTarget = true;
                AddAttackMoveOrders(orders, TargetPositionStack.Peek());
            }
        }

        public void MoveAttack(StrategicWorldState state, Queue<Order> orders)
        {
            CPos nextMove = CPos.Invalid;

            CPos attackerCenter = GeometryUtils.Center(AttackerLocationList);
            if (attackerCenter != CPos.Invalid) {
                nextMove = GeometryUtils.MoveTowards(attackerCenter, AttackTroops[0].Location, DistanceToMoveAttack, state.World.Map);
            } else {
                AggregateScoutReportData best = state.ScoutReportGrid.GetBestSurroundingCell(TargetPositionStack.Peek());
                if (best != null) {
                    nextMove = best.RelativePosition;
                }
            }

            // If we still haven't found a location, or have found our current location, search the scout report grid for another good cell anywhere on the map.
            CPos currentTarget = TargetPositionStack.Peek();
            if (nextMove == CPos.Invalid || nextMove == currentTarget) {
                AggregateScoutReportData best = state.ScoutReportGrid.GetCurrentBestFitCellExcludingPosition(currentTarget);
                if (best != null) {
                    nextMove = best.RelativePosition;
                }
            }

            
            if (nextMove != CPos.Invalid) {
                ActivateNewTargetPosition(nextMove, orders);
            }
        }

        private void ActivateNewTargetPosition(CPos nextMove, Queue<Order> orders)
        {
            AttackerLocationList.Clear();
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
