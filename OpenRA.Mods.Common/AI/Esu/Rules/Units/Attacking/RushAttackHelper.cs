using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class RushAttackHelper : INotifyDamage
    {
        private const int DistanceFromPositionToConsiderOnTarget = 6;
        /** Number of ticks to wait before moving rush attack. */
        private static int TicksUntilRushAttackMove = 400;

        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private bool HasReachedTarget = false;
        private bool HasRushForceBeenSentToFoundEnemy;
        private bool NewActorAddedToRushForce;
        private int NumActorsAddedToRushForce = 0;
        private readonly List<Actor> RushActors;
        private CPos BestTargetLocation = CPos.Invalid;

        private int LastDamageTick;
        /** Contains the collection of positions that this rush attack was damaged from or applied damage to.*/
        private readonly List<CPos> AttackerLocationList;

        public RushAttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;
            this.RushActors = new List<Actor>();
            this.AttackerLocationList = new List<CPos>();

            DamageNotifier.AddDamageNotificationListener(this);
        }

        public void UnitProduced(StrategicWorldState state, Actor producer, Actor produced)
        {
            if (NumActorsAddedToRushForce < Info.NumberOfRushUnits &&  state.OffensiveActorsExceptScouts().Contains(produced))
            {
                NumActorsAddedToRushForce++;
                RushActors.Add(produced);
                NewActorAddedToRushForce = true;
            }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            foreach (Actor actor in RushActors) {
                if (e.Attacker == actor) {
                    LastDamageTick = World.GetCurrentLocalTickCount();
                    AttackerLocationList.Add(self.Location);
                    return;
                }
            }
        }

        public void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (state.EnemyInfoList.Count == 0 || RushActors.Count == 0) {
                return;
            }
            TrimRushForce();

            // Use first enemy for now.
            EnemyInfo enemy = state.EnemyInfoList.First();
            BestTargetLocation = GetBestAvailableEnemyLocation(state, enemy);
            HasReachedTarget = HasReachedPosition(BestTargetLocation);

            if (NewActorAddedToRushForce) {
                IssueAttackMoveOrders(state, enemy, BestTargetLocation, orders);
                NewActorAddedToRushForce = false;
            } else if (!HasRushForceBeenSentToFoundEnemy && (enemy.FoundEnemyLocation != CPos.Invalid || enemy.FirstFoundEnemyStructureLocation != CPos.Invalid)) {
                IssueAttackMoveOrders(state, enemy, BestTargetLocation, orders);
                HasRushForceBeenSentToFoundEnemy = true;
            } else if (HasReachedTarget && (state.World.GetCurrentLocalTickCount() - LastDamageTick) > TicksUntilRushAttackMove) {
                MoveRushAttack(state, enemy, orders);
            }
        }

        private void MoveRushAttack(StrategicWorldState state, EnemyInfo enemy, Queue<Order> orders)
        {
            CPos attackerCenter = GeometryUtils.Center(AttackerLocationList);
            AttackerLocationList.Clear();
            LastDamageTick = state.World.GetCurrentLocalTickCount();
            if (attackerCenter == CPos.Invalid) {
                return;
            }

            CPos ourCenter = GeometryUtils.Center(RushActors);
            CPos nextMove = GeometryUtils.MoveTowards(ourCenter, attackerCenter, state.Info.DistanceToMoveAttack, state.World.Map);
            IssueAttackMoveOrders(state, enemy, nextMove, orders);
        }

        private void IssueAttackMoveOrders(StrategicWorldState state, EnemyInfo enemy, CPos targetLocation, Queue<Order> orders)
        {
            if (RushActors.Count == 0) {
                return;
            }

            foreach (Actor actor in RushActors)
            {
                var move = new Order("AttackMove", actor, false) { TargetLocation = targetLocation };
                orders.Enqueue(move);
            }
        }

        private CPos GetBestAvailableEnemyLocation(StrategicWorldState state, EnemyInfo enemy)
        {
            if (enemy.FoundEnemyLocation != CPos.Invalid) {
                return enemy.FoundEnemyLocation; 
            } else if (enemy.FirstFoundEnemyStructureLocation != CPos.Invalid) {
                return enemy.FirstFoundEnemyStructureLocation;
            } else {
                return enemy.GetBestAvailableEnemyLocation(state, SelfPlayer);
            }
        }

        private bool HasReachedPosition(CPos position)
        {
            if (position == CPos.Invalid || RushActors.Count == 0) {
                return false;
            }

            CPos attackCenter = GeometryUtils.Center(RushActors);
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

        private void TrimRushForce()
        {
            for (int i = RushActors.Count - 1; i >= 0; i--)
            {
                if (RushActors[i].IsDead || !RushActors[i].IsInWorld)
                {
                    RushActors.RemoveAt(i);
                }
            }
        }
    }
}
