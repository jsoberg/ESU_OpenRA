using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttack
    {
        private const int DistanceToMoveAttackTowardEnemy = 10;
        private const int DistanceFromPositionToConsiderOnTarget = 2;

        public List<Actor> AttackTroops;
        public int LastActionTick;

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

        public readonly int Id;

        public ActiveAttack(int id, CPos targetPosition, CPos stagedPosition, IEnumerable<Actor> attackTroops)
        {
            this.Id = id;

            this.TargetPositionStack = new Stack<CPos>();
            TargetPositionStack.Push(targetPosition);
            this.StagedPosition = stagedPosition;

            this.AttackerLocationList = new List<CPos>();
            this.AttackTroops = new List<Actor>(attackTroops);

            Log.Write(ActiveAttackController.AttackDataLogName, "Attack with ID {0} has been created, containing {1} units".F(Id, AttackTroops.Count));
        }

        public void AttackedFrom(Actor attacker, World world)
        {
            AttackerLocationList.Add(attacker.Location);
            LastActionTick = world.GetCurrentLocalTickCount();
        }

        public void AttackedTo(Actor attackingTroop, Actor actorAttacked, World world)
        {
            LastActionTick = world.GetCurrentLocalTickCount();
        }

        public bool HasReachedTargetPosition(World world)
        {
            if (TargetPositionReachedTickCount > 0) {
                return true;
            }

            CPos currentTarget = TargetPositionStack.Peek();
            if (HasReachedPosition(currentTarget))
            {
                // All troops have reached target position.
                Log.Write(ActiveAttackController.AttackDataLogName, "{0} has reached target position [{1},{2}]".F(Id, currentTarget.X, currentTarget.Y));
                TargetPositionReachedTickCount = world.GetCurrentLocalTickCount();
                return true;
            }
            return false;
        }

        public bool HasReachedStagedPosition(World world)
        {
            if (StagedPositionReachedTickCount > 0 || StagedPosition == CPos.Invalid) {
                return true;
            }

            if (HasReachedPosition(StagedPosition))
            {
                // All troops have reached staging area.
                Log.Write(ActiveAttackController.AttackDataLogName, "{0} has reached staged position [{1},{2}]".F(Id, StagedPosition.X, StagedPosition.Y));
                StagedPositionReachedTickCount = world.GetCurrentLocalTickCount();
                return true;
            }
            return false;
        }

        private bool HasReachedPosition(CPos position)
        {
            CPos attackCenter = GeometryUtils.Center(AttackTroops);
            if (((position.X - DistanceFromPositionToConsiderOnTarget) < attackCenter.X && attackCenter.X < (position.X + DistanceFromPositionToConsiderOnTarget))
                    && ((position.Y - DistanceFromPositionToConsiderOnTarget) < attackCenter.Y && attackCenter.Y < (position.Y + DistanceFromPositionToConsiderOnTarget)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void MoveStagedPosition(Queue<Order> orders, CPos newStagedPosition)
        {
            Log.Write(ActiveAttackController.AttackDataLogName, "{0} is moving staged position from [{1},{2}] to [{3},{4}]".F(Id, StagedPosition.X, StagedPosition.Y, newStagedPosition.X, newStagedPosition.Y));
            WasStagedPositionMoved = true;
            StagedPosition = newStagedPosition;
            StagedPositionReachedTickCount = 0;
            AddAttackMoveOrders(orders, StagedPosition);
        }

        public void MoveFromStagedToTarget(StrategicWorldState state, Queue<Order> orders)
        {
            if (!HasMovedFromStagedToTarget)
            {
                WasStagedPositionMoved = false;
                StagedPosition = CPos.Invalid;
                HasMovedFromStagedToTarget = true;
                LastActionTick = state.World.GetCurrentLocalTickCount();
                AddAttackMoveOrders(orders, TargetPositionStack.Peek());
            }
        }

        public void MoveAttack(StrategicWorldState state, Queue<Order> orders)
        {
            CPos nextMove = CPos.Invalid;

            CPos attackerCenter = GeometryUtils.Center(AttackerLocationList);
            if (attackerCenter != CPos.Invalid) {
                CPos ourCenter = GeometryUtils.Center(AttackTroops);
                nextMove = GeometryUtils.MoveTowards(ourCenter, attackerCenter, state.Info.DistanceToMoveAttack, state.World.Map);
                Log.Write(ActiveAttackController.AttackDataLogName, "{0} is moving toward attackers, position [{1},{2}]".F(Id, nextMove.X, nextMove.Y));
            }

            // If we still haven't found a location, search the scout report grid for another good cell anywhere on the map.
            CPos currentTarget = TargetPositionStack.Peek();
            if (nextMove == CPos.Invalid || nextMove == currentTarget) {
                AggregateScoutReportData best = state.ScoutReportGrid.GetCurrentBestFitCellExcludingPosition(currentTarget);
                // Only choose a report with higher reward than risk for attack move.
                if (best != null && (best.AverageRewardValue > best.AverageRiskValue)) {
                    nextMove = best.RelativePosition;
                    Log.Write(ActiveAttackController.AttackDataLogName, "{0} is moving to best next target cell, position [{1},{2}]".F(Id, nextMove.X, nextMove.Y));
                }
            }

            // Still haven't found a location to attack next, so move attack closer to enemy.
            if (nextMove == CPos.Invalid) {
                CPos enemyLoc = state.GetClosestEnemyLocation(currentTarget);
                nextMove = GeometryUtils.MoveTowards(currentTarget, enemyLoc, DistanceToMoveAttackTowardEnemy, state.World.Map);
                Log.Write(ActiveAttackController.AttackDataLogName, "{0} is moving toward enemy base, position [{1},{2}]".F(Id, nextMove.X, nextMove.Y));
            }

            
            if (nextMove != CPos.Invalid) {
                ActivateNewTargetPosition(state, nextMove, orders);
            }
        }

        private void ActivateNewTargetPosition(StrategicWorldState state, CPos nextMove, Queue<Order> orders)
        {
            AttackerLocationList.Clear();
            TargetPositionStack.Push(nextMove);
            TargetPositionReachedTickCount = 0;

            LastActionTick = state.World.GetCurrentLocalTickCount();
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
