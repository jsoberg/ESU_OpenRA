﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIUtils
    {
        // ========================================
        // Location Tasks
        // ========================================

        public static IEnumerable<CPos> PossibleEnemyLocationsForPlayer(World world, Player player)
        {
            var constructionYard = world.Actors.Where(a => a.Owner == player &&
                a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD).FirstOrDefault();
            if (constructionYard == null) {
                throw new NoConstructionYardException("Contruction yard not yet created");
            }

            var selfLocation = constructionYard.Location;

            CPos opposite = GeometryUtils.OppositeLocationOnMap(selfLocation, world.Map);
            CPos parallel = GeometryUtils.ParallelXLocationOnMap(selfLocation, world.Map);
            return new List<CPos> { opposite, parallel };
        }

        // ========================================
        // Production Tasks
        // ========================================

        public static bool IsAnyItemCurrentlyInProductionForCategory(World world, Player owner, string category)
        {
            IEnumerable<ProductionItem> items = ItemsCurrentlyInProductionQueuesForCategory(world, owner, category);
            return (items.Count() > 0);
        }

        public static bool IsItemCurrentlyInProductionForCategory(World world, Player owner, string category, string itemName)
        {
            IEnumerable<ProductionItem> items = ItemsCurrentlyInProductionQueuesForCategory(world, owner, category);
            foreach (ProductionItem item in items) {
                if (item.Item == itemName) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsItemCurrentlyInProduction(World world, Player owner, string itemName)
        {
            var queues = FindAllProductionQueuesForPlayer(world, owner);
            foreach (ProductionQueue q in queues) {
                if (q.CurrentItem() != null && q.CurrentItem().Item == itemName) {
                    return true;
                }
            }

            return false;
        }

        /** @return true if this item is requested to be built, int he process of being built, or is already built. */
        public static bool DoesItemCurrentlyExistOrIsBeingProducedForPlayer(StrategicWorldState state, Player owner, string item)
        {
            if (state.RequestedBuildingQueue.Contains(item)) {
                return true;
            }

            bool currentlyExists = state.World.Actors.Any(a => a.Owner == owner && a.Info.Name == item && !a.IsDead);
            return (currentlyExists) ? currentlyExists : IsItemCurrentlyInProduction(state.World, owner, item);
        }

        public static IEnumerable<ProductionItem>ItemsCurrentlyInProductionQueuesForCategory(World world, Player owner, string category)
        {
            IEnumerable<ProductionQueue> productionQueues = FindProductionQueuesForPlayerAndCategory(world, owner, category);

            List<ProductionItem> itemsInQueues = new List<ProductionItem>();
            foreach (ProductionQueue queue in productionQueues) {
                if (queue.CurrentItem() != null) {
                    itemsInQueues.Add(queue.CurrentItem());
                }
            }

            return itemsInQueues;
        }

        public static IEnumerable<ProductionQueue> FindProductionQueuesForPlayerAndCategory(World world, Player owner, string category)
        {
            return world.ActorsWithTrait<ProductionQueue>()
                .Where(a => a.Actor.Owner == owner && a.Trait.Info.Type == category && a.Trait.Enabled)
                .Select(a => a.Trait);
        }

        public static IEnumerable<ProductionQueue> FindAllProductionQueuesForPlayer(World world, Player owner)
        {
            return world.ActorsWithTrait<ProductionQueue>()
                .Where(a => a.Actor.Owner == owner && a.Trait.Enabled)
                .Select(a => a.Trait);
        }

        public static IEnumerable<ProductionQueue> FindAllProductionQueuesForPlayerExcluding(World world, Player owner, params string[] excluded)
        {
            if (excluded == null || excluded.Count() == 0) {
                return FindAllProductionQueuesForPlayer(world, owner);
            }

            return world.ActorsWithTrait<ProductionQueue>()
                .Where(a => a.Actor.Owner == owner && a.Trait.Enabled && !excluded.Contains(a.Trait.Info.Type))
                .Select(a => a.Trait);
        }

        public static bool CanBuildItemWithNameForCategory(World world, Player owner, string category, string name)
        {
            return world.ActorsWithTrait<ProductionQueue>()
                .Any(pq => pq.Actor.Owner == owner && pq.Trait.Info.Type == category && pq.Trait.Enabled && pq.Trait.BuildableItems().Any(ai => ai.Name == name));
        }

        public static int BuildingCountForPlayerOfType(World world, Player owner, string buildingName)
        {
            return world.ActorsHavingTrait<Building>()
                .Count(a => a.Owner == owner && a.Info.Name == buildingName);
        }

        public static double GetPercentageOfResourcesSpentOnProductionType(World world, Player owner, string productionType)
        {
            double totalEarned = owner.PlayerActor.Trait<PlayerResources>().Earned;
            if (totalEarned == 0) {
                return -1;
            }

            var ownedActorsWithTrait = world.ActorsHavingTrait<Buildable>().Where(a => a.Owner == owner && a.Info.TraitInfo<BuildableInfo>().Queue.Contains(productionType));
            double totalCost = 0;
            foreach (Actor a in ownedActorsWithTrait) {
                totalCost += a.Info.TraitInfoOrDefault<ValuedInfo>().Cost;
            }

            return (totalCost / totalEarned);
        }

        public static double GetCurrentResourcesForPlayer(Player owner)
        {
            PlayerResources resources = owner.PlayerActor.Trait<PlayerResources>();
            return (resources.Cash + resources.Resources); 
        }

        // ========================================
        // Actor Tasks
        // ========================================

        public static bool IsActorOfType(World world, Actor actor, string type)
        {
            IEnumerable<ProductionQueue> queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, actor.Owner, type);
            foreach (ProductionQueue queue in queues)
            {
                var producables = queue.AllItems();
                foreach (ActorInfo producable in producables)
                {
                    if (actor.Info.Name == producable.Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void AttackTargetIfVisible(StrategicWorldState state, IEnumerable<Actor> actorsToPerformAttack, string targetName)
        {
            var offensiveEnemyActors = state.EnemyActorsCache.Where(a => a.Info.Name == targetName);
            foreach (Actor actor in actorsToPerformAttack)
            {
                AttackTargetIfVisible(state, actor, offensiveEnemyActors, targetName);
            }
        }

        public static void AttackTargetIfVisible(StrategicWorldState state, Actor actorToPerformAttack, string targetName)
        {
            var offensiveEnemyActors = state.EnemyActorsCache.Where(a => a.Info.Name == targetName);
            AttackTargetIfVisible(state, actorToPerformAttack, offensiveEnemyActors, targetName);
        }

        private static void AttackTargetIfVisible(StrategicWorldState state, Actor actorToPerformAttack, IEnumerable<Actor> enemyActors, string targetName)
        {
            var visiblity = VisibilityBounds.GetCurrentVisibilityRectForActor(actorToPerformAttack);
            var visibleEnemies = enemyActors.Where(a => visiblity.ContainsPosition(a.CenterPosition));
            if (visibleEnemies.Count() > 0)
            {
                var target = visibleEnemies.First();
                var current = actorToPerformAttack.GetCurrentActivity();
                actorToPerformAttack.QueueActivity(false, new Attack(actorToPerformAttack, Target.FromPos(target.CenterPosition), true, true));
                actorToPerformAttack.QueueActivity(true, current);
            }
        }

    }

    // ========================================
    // Various Exceptions
    // ========================================

    public class NoConstructionYardException : NullReferenceException
    {
        public NoConstructionYardException(String message) : base(message) { }
    }
}
