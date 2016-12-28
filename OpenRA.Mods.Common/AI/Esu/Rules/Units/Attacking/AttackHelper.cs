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

        public AttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;
        }

        public void AddAttackOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            state.ActiveAttackController.Tick(self, state, orders);

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
            var defensiveCoverage = metric.CurrentDefenseCoverage_Simple(DEFENSIVE_COVERAGE, state.ActiveAttackController.GetActiveAttacks());

            IEnumerable<Actor> possibleAttackActors = ActorsCurrentlyAvailableForAttack(state, defensiveCoverage.ActorsNecessaryForDefense);
            AttackStrengthPredictor predictor = new AttackStrengthPredictor(metric, state);
            // TODO add more logic here
            if (predictor.PredictStrengthForAttack(bestCell.AverageRiskValue, bestCell.AverageRewardValue, possibleAttackActors, bestCell.RelativePosition) == PredictedAttackStrength.Medium) {
                state.ActiveAttackController.AddNewActiveAttack(orders, bestCell.RelativePosition, possibleAttackActors);
            }
        }

        private IEnumerable<Actor> ActorsCurrentlyAvailableForAttack(StrategicWorldState state, IEnumerable<Actor> defensiveActors)
        {
            IEnumerable<Actor> actors = World.ActorsHavingTrait<Armament>().Where(a => a.Owner == SelfPlayer && !a.IsDead 
                && !defensiveActors.Contains(a));

            return actors.Except(AllActorsInAttack(state));
        }

        private IEnumerable<Actor> AllActorsInAttack(StrategicWorldState state)
        {
            List<Actor> actors = new List<Actor>();
            IEnumerable<ActiveAttack> currentAttacks = state.ActiveAttackController.GetActiveAttacks();
            foreach (ActiveAttack attack in currentAttacks) {
                actors.Concat(actors);
            }
            return actors;
        }
    }
}
