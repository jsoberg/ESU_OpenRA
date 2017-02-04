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

        public void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
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
            // Only check for attack strength if we have new information to save on computation time.
            if (!state.CheckAttackStrengthPredictionFlag) {
                return;
            }

            var metric = new BaseLethalityMetric(state, SelfPlayer);
            var defensiveCoverage = metric.CurrentDefenseCoverage_Simple(state, Info.GetDefenseLethalityCoveragePercentage(), state.ActiveAttackController.GetActiveAttacks());

            IEnumerable<Actor> possibleAttackActors = ActorsCurrentlyAvailableForAttack(state, defensiveCoverage.ActorsNecessaryForDefense);
            AttackStrengthPredictor predictor = new AttackStrengthPredictor(metric, state);
            // TODO add more logic here
            if (predictor.PredictStrengthForAttack(bestCell.AverageRiskValue, bestCell.AverageRewardValue, possibleAttackActors, bestCell.RelativePosition) == PredictedAttackStrength.Medium) {
                ScoutReportLocationGrid reportGrid = state.ScoutReportGrid;
                CPos stagedPosition = reportGrid.GetSafeCellPositionInbetweenCells(bestCell.RelativePosition, state.SelfIntialBaseLocation);
                state.ActiveAttackController.AddNewActiveAttack(orders, bestCell.RelativePosition, stagedPosition, possibleAttackActors);
            }

            // We've checked for attack strength, so don't check again until we have new viable information.
            state.CheckAttackStrengthPredictionFlag = false;
        }

        private IEnumerable<Actor> ActorsCurrentlyAvailableForAttack(StrategicWorldState state, IEnumerable<Actor> defensiveActors)
        {
            var actorsNotInAttack = state.OffensiveActorsCache.Except(AllActorsInAttack(state));
            return actorsNotInAttack.Except(defensiveActors);
        }

        private IEnumerable<Actor> AllActorsInAttack(StrategicWorldState state)
        {
            IEnumerable<Actor> actors = new List<Actor>();
            var currentAttacks = state.ActiveAttackController.GetActiveAttacks();
            foreach (ActiveAttack attack in currentAttacks) {
                actors = actors.Concat(attack.AttackTroops);
            }
            return actors;
        }
    }
}
