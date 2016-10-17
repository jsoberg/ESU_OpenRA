using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Rules;
using OpenRA.Mods.Common.AI.Esu.Strategy;

/// <summary>
///  This class is the implementation of the modular ESU AI, with a ruleset described at the project's <see href="https://github.com/jsoberg/ESU_OpenRA/wiki/AI-Rules">GitHub Wiki</see>.
/// </summary>
namespace OpenRA.Mods.Common.AI.Esu
{
    public sealed class EsuAI : ITick, IBot, INotifyDamage, INotifyDiscovered, INotifyOtherProduction
    {
        private readonly EsuAIInfo info;
        private readonly World world;
        private readonly StrategicWorldState worldState;

        // Rulesets.
        private readonly List<BaseEsuAIRuleset> rulesets;

        private Player selfPlayer;
        private bool isEnabled;
        private int tickCount;

        public EsuAI(EsuAIInfo info, ActorInitializer init)
        {
            this.info = info;
            this.world = init.World;
            this.worldState = new StrategicWorldState();

            rulesets = new List<BaseEsuAIRuleset>();
            addRulesets();
        }

        private void addRulesets()
        {
            rulesets.Add(new BuildRuleset(world, info));
            rulesets.Add(new UnitRuleset(world, info));
        }

        IBotInfo IBot.Info
        {
            get { return info; }
        }

        void IBot.Activate(Player p)
        {
            isEnabled = true;
            selfPlayer = p;

            foreach (BaseEsuAIRuleset rs in rulesets) {
                rs.Activate(p);
            }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {

        }

        void INotifyDiscovered.OnDiscovered(Actor self, Player discoverer, bool playNotification)
        {

        }

        void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced)
        {
            var notifyOtherProductionRulesets = rulesets.Where(a => a is INotifyOtherProduction);
            foreach (INotifyOtherProduction rs in notifyOtherProductionRulesets) {
                rs.UnitProducedByOther(self, producer, produced);
            }
        }

        void ITick.Tick(Actor self)
        {
            if (!isEnabled) {
                return;
            }

            tickCount++;

            // Check for initial tick.
            if (tickCount == 1) {
                DeployMcv(self);
                return;
            }

            if (!worldState.IsInitialized) {
                worldState.Initalize(world, selfPlayer);
            } else {
                worldState.UpdateCurrentWorldState();
            }

            // Get and issue orders.
            Queue<Order> orders = new Queue<Order>();
            foreach (BaseEsuAIRuleset rs in rulesets) {
                rs.Tick(self, worldState, orders);
            }

            double currentResources = EsuAIUtils.GetCurrentResourcesForPlayer(selfPlayer);
            foreach (Order order in orders) {
                // We don't have the marked minimum resources to execute this order, so ignore it.
                if (order.OrderString == EsuAIConstants.OrderTypes.PRODUCTION_ORDER && currentResources < info.AmountOfResourcesToHaveBeforeNextProduction) {
                    OrderDenied(order);
                } else {
                    world.IssueOrder(order);
                }
            }
        }

        private void DeployMcv(Actor self)
        {
            var mcv = world.Actors.FirstOrDefault(a => a.Owner == self.Owner && a.Info.Name == "mcv");

            if (mcv != null) {
                world.IssueOrder(new Order("DeployTransform", mcv, true));
            } else {
                throw new ArgumentNullException("Cannot find MCV");
            }
        }

        private void OrderDenied(Order order)
        {
            foreach (BaseEsuAIRuleset rule in rulesets) {
                var listener = rule as IOrderDeniedListener;
                if (listener != null) {
                    listener.OnOrderDenied(order);
                }
            }
        }
    }

    public sealed class EsuAIInfo : IBotInfo, ITraitInfo
    {
        private const string AI_NAME = "ESU AI";

        // ========================================
        // Rule Tunable
        // ========================================

        [Desc("Minimum excess power we should maintain (Rule BuildPowerPlantIfBelowMinimumExcessPower)")]
        public readonly int MinimumExcessPower = 50;

        [Desc("Determines whether we should produce a scout before a refinery (Rule ShouldProduceScoutBeforeRefinery)")]
        public readonly int ShouldProduceScoutBeforeRefinery = 1;

        [Desc("Determines precentage of resources to spend on defensive buildings (Rule PercentageOfResourcesToSpendOnDefensiveBuildings)")]
        public readonly double PercentageOfResourcesToSpendOnDefensiveBuildings = 20;

        [Desc("Determines where to place defensive buildings (Rule DefensiveBuildingPlacement)")]
        public readonly int DefensiveBuildingPlacement = RuleConstants.DefensiveBuildingPlacementValues.DISTRIBUTED_TO_IMPORTANT_STRUCTURES;

        [Desc("Determines how many scouts to produce (Rule NumberOfScoutsToProduce)")]
        public readonly int NumberOfScoutsToProduce = 2;

        [Desc("Determines where to place normal buildings (Rule NormalBuildingPlacement)")]
        public readonly int NormalBuildingPlacement = RuleConstants.NormalBuildingPlacementValues.FARTHEST_FROM_ENEMY_LOCATIONS;

        [Desc("Determines amount of resources to have on hand before the next production is considered (Rule AmountOfResourcesToHaveBeforeNextProduction)")]
        public readonly int AmountOfResourcesToHaveBeforeNextProduction = 400;

        [Desc("Determines the multiplier used when calculating scout recommendations. (Rule ScoutRecommendationImportanceMultiplier)")]
        public readonly int ScoutRecommendationImportanceMultiplier = 10;

        public float GetScoutRecommendationImportanceMultiplier()
        {
            return (ScoutRecommendationImportanceMultiplier / 10);
        }

        // ========================================
        // Static
        // ========================================
        [Desc("Radius in cells around the center of the base to expand.")]
        public readonly int MaxBaseRadius = 20;

        string IBotInfo.Name
        {
            get { return AI_NAME; }
        }

        object ITraitInfo.Create(ActorInitializer init)
        {
            return new EsuAI(this, init);
        }
    }

    public interface IOrderDeniedListener
    {
        void OnOrderDenied(Order order);
    }
}
