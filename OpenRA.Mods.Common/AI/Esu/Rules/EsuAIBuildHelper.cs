using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public class EsuAIBuildHelper
    {
        private readonly MersenneTwister Random = new MersenneTwister();

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
            var productionQueues = EsuAIUtils.FindAllProductionQueuesForPlayer(world, selfPlayer);
            foreach (ProductionQueue queue in productionQueues) {
                var currentBuilding = queue.CurrentItem();
                if (currentBuilding == null || !currentBuilding.Done) {
                    continue;
                }

                var location = FindBuildLocation(currentBuilding.Item);

                // TODO: handle null location found (cancel production?)
                if (location != CPos.Invalid) {
                    orders.Enqueue(new Order("PlaceBuilding", selfPlayer.PlayerActor, false)
                    {
                        TargetLocation = location,
                        TargetString = currentBuilding.Item,
                        TargetActor = queue.Actor,
                        SuppressVisualFeedback = true
                    });
                }
            }
        }

        [Desc("Returns an acceptable build location for the specified type.")]
        public CPos FindBuildLocation(string actorType)
        {
            var type = GetBuildingTypeForActorType(actorType);
            var baseCenter = GetRandomBaseCenter();

            switch (type) {
                case BuildingType.Refinery:
                    // Try and place the refinery near a resource field
                    return FindBuildableLocationNearResources();
                case BuildingType.Building:
                    return FindRandomBuildableLocation(baseCenter, 0, info.MaxBaseRadius, actorType);
                case BuildingType.Defense:
                    return FindRandomBuildableLocation(baseCenter, 0, info.MaxBaseRadius, actorType);
            }

            // Can't find a build location
            return CPos.Invalid;
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
        public CPos FindBuildableLocationNearResources()
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
                    var found = FindFirstBuildableLocation(r, 0, info.MaxBaseRadius, EsuAIConstants.Buildings.ORE_REFINERY);
                    if (found != CPos.Invalid) {
                        return found;
                    }
                }
            }
            return CPos.Invalid;
        }

        [Desc("Attempts to find the first buildable location close to base center where a building can be placed.")]
        private CPos FindFirstBuildableLocation(CPos center, int minRange, int maxRange, string actorType)
        {
            BuildingInfo bi = GetBuildingInfoForActorType(actorType);
            var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);

            foreach (var cell in cells) {
                if (!world.CanPlaceBuilding(actorType, bi, cell, null))
                    continue;
                if (!bi.IsCloseEnoughToBase(world, selfPlayer, actorType, cell))
                    continue;

                return cell;
            }
            return CPos.Invalid;
        }

        [Desc("Attempts to find a random location close to base center where a building can be placed.")]
        private CPos FindRandomBuildableLocation(CPos center, int minRange, int maxRange, string actorType)
        {
            BuildingInfo bi = GetBuildingInfoForActorType(actorType);
            var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);

            List<CPos> usableCells = new List<CPos>();
            foreach (var cell in cells) {
                if (!world.CanPlaceBuilding(actorType, bi, cell, null))
                    continue;
                if (!bi.IsCloseEnoughToBase(world, selfPlayer, actorType, cell))
                    continue;

                usableCells.Add(cell);
            }
            return usableCells.Count == 0 ? CPos.Invalid : usableCells.Random(Random);
        }

        private BuildingInfo GetBuildingInfoForActorType(string actorType)
        {
            var bi = world.Map.Rules.Actors[actorType].TraitInfoOrDefault<BuildingInfo>();
            if (bi == null) {
                throw new SystemException("Unsupported actor type: " + actorType);
            }
            return bi;
        }

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
