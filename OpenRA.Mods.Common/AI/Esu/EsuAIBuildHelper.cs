using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu
{
    public class EsuAIBuildHelper
    {
        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        public EsuAIBuildHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;
        }

        [Desc("Adds order to place building if any buildings are complete.")]
        public void PlaceBuildingsIfComplete(Queue<Order> orders)
        {
            IEnumerable<ProductionQueue> productionQueues = EsuAIUtils.FindProductionQueues(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING);
            foreach (ProductionQueue queue in productionQueues) {
                var currentBuilding = queue.CurrentItem();
                if (currentBuilding == null || !currentBuilding.Done) {
                    continue;
                }

                var location = FindBuildLocation(currentBuilding.Item);

                // TODO: handle null location found (cancel production?)
                if (location != null) {

                    orders.Enqueue(new Order("PlaceBuilding", selfPlayer.PlayerActor, false)
                    {
                        TargetLocation = location.Value,
                        TargetString = currentBuilding.Item,
                        TargetActor = queue.Actor,
                        SuppressVisualFeedback = true
                    });
                }
            }
        }

        [Desc("Returns an acceptable build location for the specified type.")]
        public CPos? FindBuildLocation(string actorType)
        {
            var type = GetBuildingTypeForActorType(actorType);
            switch (type) {
                case BuildingType.Defense:
                // TODO find optimal placement.
                case BuildingType.Refinery:
                    // Try and place the refinery near a resource field
                    return FindBuildableLocationNearResources();
                case BuildingType.Building:
                    var baseCenter = GetRandomBaseCenter();
                    return FindRandomBuildableLocation(baseCenter, 0, info.MaxBaseRadius, actorType);
            }

            // Can't find a build location
            return null;
        }

        private BuildingType GetBuildingTypeForActorType(string actorType)
        {
            var type = BuildingType.Building;
            if (world.Map.Rules.Actors[actorType].HasTraitInfo<AttackBaseInfo>()) {
                type = BuildingType.Defense;
            } else if (world.Map.Rules.Actors[actorType].HasTraitInfo<RefineryInfo>()) {
                type = BuildingType.Refinery;
            }

            return type;
        }

        [Desc("Attempts to find a buildable location close to resources that are nearest to the base.")]
        public CPos? FindBuildableLocationNearResources()
        {
            var baseCenter = GetRandomBaseCenter();

            var tileset = world.Map.Rules.TileSet;
            var resourceTypeIndices = new BitArray(tileset.TerrainInfo.Length);
            foreach (var t in world.Map.Rules.Actors["world"].TraitInfos<ResourceTypeInfo>())
                resourceTypeIndices.Set(tileset.GetTerrainIndex(t.TerrainType), true);

            // We want to start the seach close to base center, expanding further out until we find something.
            int maxRad_4 = info.MaxBaseRadius / 4;
            for (int radius = maxRad_4 / 4; radius <= info.MaxBaseRadius; radius += maxRad_4)
            {

                // TODO: Figure out obstacles in the way (i.e water separating ore from harvester, cliffs etc).
                var nearbyResources = world.Map.FindTilesInAnnulus(baseCenter, 0, info.MaxBaseRadius)
                    .Where(a => resourceTypeIndices.Get(world.Map.GetTerrainIndex(a)))
                    .Shuffle(Random).Take(6);

                foreach (var r in nearbyResources)
                {
                    var found = FindRandomBuildableLocation(r, 0, info.MaxBaseRadius, EsuAIConstants.Buildings.ORE_REFINERY);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        [Desc("Attempts to find a random location close to base center where a building can be placed.")]
        private CPos? FindRandomBuildableLocation(CPos center, int minRange, int maxRange, string actorType)
        {
            var bi = world.Map.Rules.Actors[actorType].TraitInfoOrDefault<BuildingInfo>();
            if (bi == null)
                return null;

            var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);
            foreach (var cell in cells) {

                if (!world.CanPlaceBuilding(actorType, bi, cell, null))
                    continue;
                if (!bi.IsCloseEnoughToBase(world, selfPlayer, actorType, cell))
                    continue;

                return cell;
            }
            return null;
        }

        private readonly MersenneTwister Random = new MersenneTwister();

        // TODO: This was copied from HackyAI; We want to be smarter about this than 
        // just building at a random construction yard, but this will do for now.
        private CPos GetRandomBaseCenter()
        {
            var randomConstructionYard = world.Actors.Where(a => a.Owner == selfPlayer &&
                a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD)
                .RandomOrDefault(Random);

            // TODO: Possible NPE
            return randomConstructionYard.Location;
        }
    }
}
