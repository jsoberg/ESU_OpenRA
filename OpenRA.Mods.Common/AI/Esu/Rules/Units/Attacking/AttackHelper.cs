using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Defense;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class AttackHelper
    {
        private const double DEFENSIVE_COVERAGE = .2;
        private const int TICKS_TO_CHECK = 10;

        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private readonly ActiveAttackController AttackController;

        public AttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;
            this.AttackController = new ActiveAttackController();
        }

        public void AddAttackOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (World.GetCurrentLocalTickCount() % TICKS_TO_CHECK == 0) {
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

            IssueAttackIfViable(state, orders, bestCell);
        }

        private void IssueAttackIfViable(StrategicWorldState state, Queue<Order> orders, AggregateScoutReportData bestCell)
        {
            var metric = new BaseLethalityMetric(World, SelfPlayer);
            var defensiveCoverage = metric.CurrentDefenseCoverage_Simple(DEFENSIVE_COVERAGE, AttackController.GetActiveAttacks());

            IEnumerable<Actor> possibleAttackActors = ActorsCurrentlyAvailableForAttack(defensiveCoverage.ActorsNecessaryForDefense);
            AttackStrengthPredictor predictor = new AttackStrengthPredictor(metric, state);
            // TODO add more logic here
            if (predictor.PredictStrengthForAttack(bestCell.AverageRiskValue, bestCell.AverageRewardValue, possibleAttackActors, bestCell.RelativePosition) == PredictedAttackStrength.Medium) {
                IssueAttackOrders(orders, possibleAttackActors, bestCell.RelativePosition);
            }
        }

        private IEnumerable<Actor> ActorsCurrentlyAvailableForAttack(IEnumerable<Actor> defensiveActors)
        {
            IEnumerable<Actor> actors = World.ActorsHavingTrait<Armament>().Where(a => a.Owner == SelfPlayer && !a.IsDead 
                && !defensiveActors.Contains(a));

            return actors.Except(AllActorsInAttack());
        }

        private IEnumerable<Actor> AllActorsInAttack()
        {
            List<Actor> actors = new List<Actor>();
            List<IssuedAttack> currentAttacks = AttackController.GetActiveAttacks();
            foreach (IssuedAttack attack in currentAttacks) {
                actors.Concat(actors);
            }
            return actors;
        }

        private void IssueAttackOrders(Queue<Order> orders, IEnumerable<Actor> attackActors, CPos targetPosition)
        {
            AddAttackMoveOrders(orders, attackActors, targetPosition);
            AttackController.AddNewActiveAttack(targetPosition, attackActors);
        }

        private void AddAttackMoveOrders(Queue<Order> orders, IEnumerable<Actor> attackActors, CPos targetPosition)
        {
            foreach (Actor actor in attackActors) {
                var move = new Order("AttackMove", actor, false) { TargetLocation = targetPosition };
                orders.Enqueue(move);
            }
        }
    }
}
