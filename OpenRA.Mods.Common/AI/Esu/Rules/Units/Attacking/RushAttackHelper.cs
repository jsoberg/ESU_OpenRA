using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class RushAttackHelper
    {
        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private bool HasRushForceBeenSentToFoundEnemy;
        private bool NewActorAddedToRushForce;
        private readonly List<Actor> RushActors;

        public RushAttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;
            this.RushActors = new List<Actor>();
        }

        public void UnitProduced(StrategicWorldState state, Actor producer, Actor produced)
        {
            if (RushActors.Count < Info.NumberOfRushUnits &&  state.OffensiveActorsExceptScouts().Contains(produced))
            {
                RushActors.Add(produced);
                NewActorAddedToRushForce = true;
            }
        }

        public void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (state.EnemyInfoList.Count == 0) {
                return;
            }

            // Use first enemy for now.
            EnemyInfo enemy = state.EnemyInfoList.First();

            if (NewActorAddedToRushForce) {
                IssueAttackMoveOrders(state, enemy, orders);
                NewActorAddedToRushForce = false;
            } else if (!HasRushForceBeenSentToFoundEnemy && (enemy.FoundEnemyLocation != CPos.Invalid || enemy.FirstFoundEnemyStructureLocation != CPos.Invalid)) {
                IssueAttackMoveOrders(state, enemy, orders);
                HasRushForceBeenSentToFoundEnemy = true;
            }
        }

        private void IssueAttackMoveOrders(StrategicWorldState state, EnemyInfo enemy, Queue<Order> orders)
        {
            CPos attackLocation = GetBestAvailableEnemyLocation(state, enemy);
            foreach (Actor actor in RushActors)
            {
                var move = new Order("AttackMove", actor, false) { TargetLocation = attackLocation };
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
    }
}
