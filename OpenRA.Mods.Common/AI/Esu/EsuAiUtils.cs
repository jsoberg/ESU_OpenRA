using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIUtils
    {
        // ========================================
        // Visibility Tasks
        // ========================================

        public static VisibilityBounds CalculateCurrentVisibleAreaForPlayer(World world, Player owner)
        {
            // Get all Actors owned by specified owner that have the RevealsShroud trait.
            var ownedActors = world.Actors.Where(a => a.Owner == owner && a.IsInWorld
                && !a.IsDead && a.TraitOrDefault<RevealsShroud>() != null);

            VisibilityBounds bounds = new VisibilityBounds();
            foreach (Actor actor in ownedActors)
            {
                WDist range = actor.Trait<RevealsShroud>().Range;
                Rect visibleRect = new Rect(actor.CenterPosition, range.Length);
                bounds.AddRect(visibleRect);
            }

            return bounds;
        }

        // ========================================
        // Location Tasks
        // ========================================

        public static CPos OppositeBaseLocationOfPlayer(World world, Player player)
        {
            var constructionYard = world.Actors.Where(a => a.Owner == player &&
                a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD).FirstOrDefault();
            if (constructionYard == null) {
                throw new NoConstructionYardException("Contruction yard not yet created");
            }

            var selfLocation = constructionYard.Location;

            return GeometryUtils.OppositeLocationOnMap(selfLocation, world.Map);
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

        public static IEnumerable<ProductionItem> ItemsCurrentlyInProductionQueuesForCategory(World world, Player owner, string category)
        {
            IEnumerable<ProductionQueue> productionQueues = FindProductionQueues(world, owner, category);

            List<ProductionItem> itemsInQueues = new List<ProductionItem>();
            foreach (ProductionQueue queue in productionQueues) {
                if (queue.CurrentItem() != null) {
                    itemsInQueues.Add(queue.CurrentItem());
                }
            }

            return itemsInQueues;
        }

        public static IEnumerable<ProductionQueue> FindProductionQueues(World world, Player owner, string category)
        {
            return world.ActorsWithTrait<ProductionQueue>()
                .Where(a => a.Actor.Owner == owner && a.Trait.Info.Type == category && a.Trait.Enabled)
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
    }

    // ========================================
    // Various Exceptions
    // ========================================

    public class NoConstructionYardException : NullReferenceException
    {
        public NoConstructionYardException(String message) : base(message) { }
    }
}
