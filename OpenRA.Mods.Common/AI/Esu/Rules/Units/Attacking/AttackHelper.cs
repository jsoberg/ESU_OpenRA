 using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Defense;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class AttackHelper
    {
        private const int TICKS_TO_CHECK = 10;

        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private readonly PathFinder PathFinder;

        public AttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;

            this.PathFinder = new PathFinder(world);
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
            if (predictor.PredictStrengthForAttack(bestCell.AverageRiskValue, bestCell.AverageRewardValue, possibleAttackActors, bestCell.RelativePosition) 
                >= (PredictedAttackStrength) Info.PredictedAttackStrengthNeededToLaunchAttack)
            {
                ScoutReportLocationGrid reportGrid = state.ScoutReportGrid;
                CPos stagedPosition = reportGrid.GetSafeCellPositionInbetweenCells(bestCell.RelativePosition, state.SelfIntialBaseLocation);
                stagedPosition = ClosestMoveableCell(state, possibleAttackActors, stagedPosition, 0);
                CPos targetPosition = ClosestMoveableCell(state, possibleAttackActors, bestCell.RelativePosition, 0);
                state.ActiveAttackController.AddNewActiveAttack(orders, targetPosition, stagedPosition, possibleAttackActors);
            }

            // We've checked for attack strength, so don't check again until we have new information.
            state.CheckAttackStrengthPredictionFlag = false;
        }

        // Note: In an attempt to ensure that we don't have an infinite recursive loop, we only try and find a moveable cell 4 times.
        private CPos ClosestMoveableCell(StrategicWorldState state, IEnumerable<Actor> possibleAttackActors, CPos target, int numTries)
        {
            if (numTries == 4) {
                return target;
            }

            CPos start = state.SelfIntialBaseLocation;
            Actor first = possibleAttackActors.First();

            var positions = PathFinder.FindUnitPath(start, target, first);
            if (positions.Count == 0) {
                // Move closer and try again.
                target = GeometryUtils.MoveTowards(target, start, 2, state.World.Map);
                return ClosestMoveableCell(state, possibleAttackActors, target, numTries + 1);
            } else {
                return positions.First();
            }
        }

        private IEnumerable<Actor> ActorsCurrentlyAvailableForAttack(StrategicWorldState state, IEnumerable<Actor> defensiveActors)
        {
            var actorsNotInAttack = state.OffensiveActorsExceptScouts().Except(AllActorsInAttack(state));
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
