using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Defense;
using static OpenRA.Mods.Common.AI.Esu.Strategy.ScoutReportLocationGrid;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class AttackHelper
    {
        private const double DEFENSIVE_COVERAGE = .2;
        private const int TICKS_TO_CHECK = 10;

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly List<AttackInAction> CurrentAttacks;

        public AttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;

            this.CurrentAttacks = new List<AttackInAction>();
        }

        public void AddAttackOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (world.GetCurrentLocalTickCount() % TICKS_TO_CHECK == 0) {
                CheckStrategicStateForAttack(state, orders);
            }
        }

        private void CheckStrategicStateForAttack(StrategicWorldState state, Queue<Order> orders)
        {
            ScoutReportLocationGrid reportGrid = state.ScoutReportGrid;
            AggregateScoutReportData bestCell = reportGrid.GetCurrentBestFitCell();
            if (bestCell == null) {
                // We have no cell to possibly attack, continue.
                return;
            }

            var metric = new BaseLethalityMetric(world, selfPlayer);
            var defensiveCoverage = metric.CurrentDefenseCoverage_Simple(DEFENSIVE_COVERAGE, CurrentAttacks);
            // We have enough lethality to defend.
            if (defensiveCoverage.AdditionalLethalityNeededToDefend < 0) {
                IssueAttackWithDefensiveActors(defensiveCoverage.ActorsNecessaryForDefense, state, orders, bestCell.RelativePosition);
            }
        }

        private void IssueAttackWithDefensiveActors(IEnumerable<Actor> defensiveActors, StrategicWorldState state, Queue<Order> orders, CPos targetPosition)
        {
            IEnumerable<Actor> attackActors = ActorsCurrentlyAvailableForAttack(defensiveActors);
            //AddCreateGroupOrder(orders, attackActors);
            AddAttackMoveOrders(orders, attackActors, targetPosition);
            CurrentAttacks.Add(new AttackInAction(targetPosition, attackActors));
        }

        private IEnumerable<Actor> ActorsCurrentlyAvailableForAttack(IEnumerable<Actor> defensiveActors)
        {
            IEnumerable<Actor> actors = world.ActorsHavingTrait<Armament>().Where(a => a.Owner == selfPlayer && !a.IsDead 
                && !defensiveActors.Contains(a));

            return actors.Except(AllActorsInAttack());
        }

        private IEnumerable<Actor> AllActorsInAttack()
        {
            List<Actor> actors = new List<Actor>();
            foreach (AttackInAction attack in CurrentAttacks) {
                actors.Concat(actors);
            }
            return actors;
        }

        private void AddCreateGroupOrder(Queue<Order> orders, IEnumerable<Actor> actorsToGroup)
        {
            var createGroupOrder =  new Order("CreateGroup", selfPlayer.PlayerActor, false) {
                TargetString = actorsToGroup.Select(a => a.ActorID).JoinWith(",")
            };
            orders.Enqueue(createGroupOrder);
        }

        private void AddAttackMoveOrders(Queue<Order> orders, IEnumerable<Actor> attackActors, CPos targetPosition)
        {
            foreach (Actor actor in attackActors) {
                var move = new Order("AttackMove", actor, false) { TargetLocation = targetPosition };
                orders.Enqueue(move);
            }
        }

        public class AttackInAction
        {
            public CPos TargetPosition;
            public IEnumerable<Actor> AttackTroops;

            public AttackInAction(CPos targetPosition, IEnumerable<Actor> attackTroops)
            {
                this.TargetPosition = targetPosition;
                this.AttackTroops = attackTroops;
            }
        }
    }
}
