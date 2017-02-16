﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Rules;
using OpenRA.Mods.Common.AI.Esu.Rules.Units;
using OpenRA.Mods.Common.AI.Esu.Rules.Buildings;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;
using OpenRA.Mods.Common.AI.Esu.Database;
using System.Reflection;
using OpenRA.Mods.Common.AI.Esu.Rules.Resources;

/// <summary>
///  This class is the implementation of the modular ESU AI, with a ruleset described at the project's <see href="https://github.com/jsoberg/ESU_OpenRA/wiki/AI-Rules">GitHub Wiki</see>.
/// </summary>
namespace OpenRA.Mods.Common.AI.Esu
{
    public sealed class EsuAI : ITick, IBot, INotifyDamage, INotifyAppliedDamage, INotifyDiscovered, INotifyOtherProduction, INotifyProduction
    {
        private readonly EsuAIInfo Info;
        private readonly World World;
        private readonly StrategicWorldState State;
        private readonly AsyncUnitDamageInformationLogger UnitDamageInformationLogger;

        // Rulesets.
        private readonly List<BaseEsuAIRuleset> Rulesets;

        private Player SelfPlayer;
        private bool IsEnabled;
        private int TickCount;

        public EsuAI(EsuAIInfo info, ActorInitializer init)
        {
            this.Info = info;
            this.World = init.World;
            this.State = new StrategicWorldState();
            this.UnitDamageInformationLogger = new AsyncUnitDamageInformationLogger();

            Rulesets = new List<BaseEsuAIRuleset>();
            addRulesets();
        }

        private void addRulesets()
        {
            Rulesets.Add(new BuildRuleset(World, Info));
            Rulesets.Add(new UnitRuleset(World, Info));
            Rulesets.Add(new ResourceGatheringRuleset(World, Info));
        }

        IBotInfo IBot.Info
        {
            get { return Info; }
        }

        void IBot.Activate(Player p)
        {
            IsEnabled = true;
            SelfPlayer = p;

            foreach (BaseEsuAIRuleset rs in Rulesets) {
                rs.Activate(p);
            }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            UnitDamageInformationLogger.QueueUnitDamageData(new UnitDamageData(self, e));
            DamageNotifier.Damaged(self, e);
        }

        void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
        {
            UnitDamageInformationLogger.QueueUnitDamageData(new UnitDamageData(damaged, e));
            DamageNotifier.Damaged(damaged, e);
        }

        void INotifyDiscovered.OnDiscovered(Actor self, Player discoverer, bool playNotification)
        {

        }

        void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
        {
            OnUnitProduced(self, other);
        }

        void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced)
        {
            OnUnitProduced(producer, produced);
        }

        private void OnUnitProduced(Actor producer, Actor produced)
        {
            // We've produced a new unit, so set the flag to check for attack strength.
            if (produced.Owner == SelfPlayer) {
                State.CheckAttackStrengthPredictionFlag = true;
            }

            // Inform the world state.
            State.UnitProduced(producer, produced);

            var notifyOtherProductionRulesets = Rulesets.Where(a => a is IUnitProduced);
            foreach (IUnitProduced rs in notifyOtherProductionRulesets)
            {
                rs.OnUnitProduced(producer, produced);
            }
        }

        void ITick.Tick(Actor self)
        {
            if (!IsEnabled) {
                return;
            }

            TickCount++;

            // Check for initial tick.
            if (TickCount == 1) {
                DeployMcv(self);
                return;
            }
            UnitDamageInformationLogger.Tick(World);

            if (!State.IsInitialized)
            {
                State.Initalize(World, Info, SelfPlayer);
            }
            State.Tick();

            // Get and issue orders.
            Queue<Order> orders = new Queue<Order>();
            foreach (BaseEsuAIRuleset rs in Rulesets) {
                rs.Tick(self, State, orders);
            }
            IssueOrders(orders);
        }

        private void IssueOrders(Queue<Order> orders)
        {
            double currentResources = EsuAIUtils.GetCurrentResourcesForPlayer(SelfPlayer);
            foreach (Order order in orders)
            {
                // We don't have the marked minimum resources to execute this order, so ignore it.
                if (order.OrderString == EsuAIConstants.OrderTypes.PRODUCTION_ORDER && currentResources < Info.AmountOfResourcesToHaveBeforeNextProduction)
                {
                    OrderDenied(order);
                }
                else
                {
                    World.IssueOrder(order);
                }
            }
        }

        private void DeployMcv(Actor self)
        {
            var mcv = World.Actors.FirstOrDefault(a => a.Owner == self.Owner && a.Info.Name == "mcv");

            if (mcv != null) {
                World.IssueOrder(new Order("DeployTransform", mcv, true));
            } else {
                throw new ArgumentNullException("Cannot find MCV");
            }
        }

        private void OrderDenied(Order order)
        {
            foreach (BaseEsuAIRuleset rule in Rulesets) {
                var listener = rule as IOrderDeniedListener;
                if (listener != null) {
                    listener.OnOrderDenied(order);
                }
            }
        }
    }

    public sealed class EsuAIInfo : IBotInfo, ITraitInfo
    {
        private const string DEFAULT_AI_NAME = "ESU AI";

        public readonly string Name = DEFAULT_AI_NAME;

        // ========================================
        // Rule Tunable
        // ========================================

        [Desc("Minimum excess power we should maintain (Rule BuildPowerPlantIfBelowMinimumExcessPower)")]
        public readonly int MinimumExcessPower = 50;

        [Desc("Determines whether we should produce a scout before a refinery (Rule ShouldProduceScoutBeforeRefinery)")]
        public readonly int ShouldProduceScoutBeforeRefinery = 1;

        [Desc("Determines precentage of resources to spend on defensive buildings (Rule PercentageOfResourcesToSpendOnDefensiveBuildings)")]
        public readonly double PercentageOfResourcesToSpendOnDefensiveBuildings = 5;

        [Desc("Determines where to place defensive buildings (Rule DefensiveBuildingPlacement)")]
        public readonly int DefensiveBuildingPlacement = RuleConstants.DefensiveBuildingPlacementValues.DISTRIBUTED_TO_IMPORTANT_STRUCTURES;

        [Desc("Determines how many scouts to produce (Rule NumberOfScoutsToProduce)")]
        public readonly int NumberOfScoutsToProduce = 2;

        [Desc("Determines where to place normal buildings (Rule NormalBuildingPlacement)")]
        public readonly int NormalBuildingPlacement = RuleConstants.NormalBuildingPlacementValues.FARTHEST_FROM_ENEMY_LOCATIONS;

        [Desc("Determines amount of resources to have on hand before the next production is considered (Rule AmountOfResourcesToHaveBeforeNextProduction)")]
        public readonly int AmountOfResourcesToHaveBeforeNextProduction = 200;

        [Desc("Determines the multiplier used when calculating scout recommendations. (Rule ScoutRecommendationImportanceMultiplier)")]
        public readonly int ScoutRecommendationImportanceMultiplier = 10;

        public float GetScoutRecommendationImportanceMultiplier()
        {
            return ScoutRecommendationImportanceMultiplier == 0 ? 1 : ((float)ScoutRecommendationImportanceMultiplier / 10f);
        }

        [Desc("Minimum number of refineries to have built.")]
        public readonly int MinNumRefineries = 2;

        [Desc("Minimum number of active harvesters to have in the game.")]
        public readonly int MinNumHarvesters = 3;

        [Desc("Amount of resources we hold before we consider it to be an excess amount.")]
        public readonly int ExcessResourceLevel = 800;

        [Desc("Determines the multiplier used for viewed harvesters when calculating scout recommendations.")]
        public readonly int HarvesterScoutRecommendationImportanceMultiplier = 15;

        public float GetHarvesterScoutRecommendationImportanceMultiplier()
        {
            return HarvesterScoutRecommendationImportanceMultiplier == 0 ? 1 :((float) HarvesterScoutRecommendationImportanceMultiplier / 10f);
        }

        [Desc("Determines the percentage of lethality coverage we want to hold at the base for defense.")]
        public readonly int DefenseLethalityCoverage = 2;

        public float GetDefenseLethalityCoveragePercentage()
        {
            return ((float)DefenseLethalityCoverage / 10f);
        }

        [Desc("Determines the distance to move an attack when moving toward damaged enemy units.")]
        public int DistanceToMoveAttack = 8;

        [Desc("Determines the probability (in percent) that we will build a unit randomly rather than based on the compiled distribution.")]
        public int UnitProductionRandomPercent = 1;

        public float GetUnitProductionRandomPercentage()
        {
            return ((float) UnitProductionRandomPercent / 10f);
        }

        [Desc("Determines the attack strength to be predicted before launching an attack.")]
        public int PredictedAttackStrengthNeededToLaunchAttack = (int) PredictedAttackStrength.Medium;

        [Desc("Minimum lethality before we'll consider an attack.")]
        public int MinimumLethality = 400;

        [Desc("Lethality step to consider our available lethality to be on the next level.")]
        public int LethalityStep = 100;

        [Desc("Percentage of vehicle units to build vs infantry")]
        public int PercentageOfVehiclesToProduceForDistribution = 15;

        public float GetPercentageOfVehiclesToProduce()
        {
            return ((float) PercentageOfVehiclesToProduceForDistribution) / 100f;
        }

        [Desc("Earning under this amount of resources within the specified tick span will result in a new harvester/ore refinery being built")]
        public int EarnedResourcesThreshold = 4000;

        // ========================================
        // Static
        // ========================================
        [Desc("Radius in cells around the center of the base to expand.")]
        public readonly int MaxBaseRadius = 20;

        string IBotInfo.Name
        {
            get { DebugLogFields(); return Name; }
        }

        object ITraitInfo.Create(ActorInitializer init)
        {
            return new EsuAI(this, init);
        }

        private bool WasLogged = false;

        private void DebugLogFields()
        {
            if (WasLogged)
            {
                return;
            }

            // Output values for all of our modifiable GA values.
            FieldInfo[] myFields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in myFields)
            {
                if (field.Name == "MaxBaseRadius")
                    continue;

                Log.Write("debug", "{0} = {1}".F(field.Name, field.GetValue(this)));
            }

            WasLogged = true;
        }
    }

    public interface IOrderDeniedListener
    {
        void OnOrderDenied(Order order);
    }

    public interface IUnitProduced
    {
        void OnUnitProduced(Actor producer, Actor produced);
    }
}
