using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Rules;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Strategy;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Buildings
{
    public class BuildHelper
    {
        // Cooldown in ticks to wait before attempting to place a building again.
        private const int MAX_PLACEMENT_COOLDOWN = 4;

        private readonly MersenneTwister Random = new MersenneTwister();

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private int SetRallyPointTick;
        private int placementCooldown;

        public BuildHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;
        }

        [Desc("Adds order to place building if any buildings are complete.")]
        public void PlaceBuildingsIfComplete(StrategicWorldState state, Queue<Order> orders)
        {
            // We don't want to be trying to place the same building over and over again before the initial order actually goes through.
            placementCooldown--;
            if (placementCooldown > 0) {
                return;
            }

            var productionQueues = EsuAIUtils.FindAllProductionQueuesForPlayerExcluding(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
            foreach (ProductionQueue queue in productionQueues) {
                var currentBuilding = queue.CurrentItem();
                if (currentBuilding == null || !currentBuilding.Done || !IsBuilding(currentBuilding.Item)) {
                    continue;
                }

                var location = FindBuildLocation(state, currentBuilding.Item);

                // TODO: handle null location found (cancel production?)
                if (location != CPos.Invalid) {
                    placementCooldown = MAX_PLACEMENT_COOLDOWN;

                    orders.Enqueue(new Order("PlaceBuilding", selfPlayer.PlayerActor, false)
                    {
                        TargetLocation = location,
                        TargetString = currentBuilding.Item,
                        TargetActor = queue.Actor,
                        SuppressVisualFeedback = true
                    });

                    state.CheckNewDefensiveStructureFlag = true;
                    SetRallyPointTick = world.GetCurrentLocalTickCount() + 20;
                }
            }
            
            if (SetRallyPointTick != 0 && SetRallyPointTick >= world.GetCurrentLocalTickCount())
            {
                SetRallyPointsForNewProductionBuildings(orders);
                // Periodically reset the rally points for buildings, to distribute them through the base. 
                SetRallyPointTick = world.GetCurrentLocalTickCount() + 40;
            }
        }

        public void SetRallyPointsForNewProductionBuildings(Queue<Order> orders)
        {
            var baseCenter = GetRandomBaseCenter();
            if (baseCenter == CPos.Invalid) {
                return;
            }

            foreach (var rp in world.ActorsWithTrait<RallyPoint>())
            {
                if (rp.Actor.Owner == selfPlayer &&
                    !IsRallyPointValid(rp.Trait.Location, rp.Actor.Info.TraitInfoOrDefault<BuildingInfo>()))
                {
                    Order order = new Order("SetRallyPoint", rp.Actor, false)
                    {
                        TargetLocation = ChooseRallyLocationNear(baseCenter, rp.Actor)
                    };
                    orders.Enqueue(order);
                }
            }
        }

        private CPos ChooseRallyLocationNear(CPos location, Actor producer)
        {
            var possibleRallyPoints = world.Map.FindTilesInCircle(location, 6)
                .Where(c => IsRallyPointValid(c, producer.Info.TraitInfoOrDefault<BuildingInfo>()));

            if (!possibleRallyPoints.Any())
            {
                return producer.Location;
            }

            return possibleRallyPoints.Random(Random);
        }

        private bool IsRallyPointValid(CPos x, BuildingInfo info)
        {
            return info != null && world.IsCellBuildable(x, info);
        }

        private bool IsBuilding(string actorType)
        {
            return world.Map.Rules.Actors[actorType].TraitInfoOrDefault<BuildingInfo>() != null;
        }

        [Desc("Returns an acceptable build location for the specified type.")]
        public CPos FindBuildLocation(StrategicWorldState state, string actorType)
        {
            var type = GetBuildingTypeForActorType(actorType);
            var baseCenter = GetRandomBaseCenter();
            if (baseCenter == CPos.Invalid) {
                return CPos.Invalid;
            }

            switch (type) {
                case BuildingType.Refinery:
                    // Try and place the refinery near a resource field
                    return FindBuildableLocationNearResources();
                case BuildingType.Building:
                    return FindNormalBuildingPlacement(state, baseCenter, actorType);
                case BuildingType.Defense:
                    return FindDefensiveBuildingPlacement(baseCenter, actorType);
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
            if (baseCenter == CPos.Invalid) {
                return CPos.Invalid;
            }

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

            return (randomConstructionYard != null) ? randomConstructionYard.Location : CPos.Invalid;
        }

        [Desc("Attempts to find a location to place a defensive structure, based on the DefensiveBuildingPlacement rule.")]
        private CPos FindDefensiveBuildingPlacement(CPos baseCenter, string actorType)
        {
            switch (info.DefensiveBuildingPlacement) {
                case RuleConstants.DefensiveBuildingPlacementValues.CLOSEST_TO_CONSTRUCTION_YARD:
                    return FindFirstBuildableLocation(baseCenter, 0, info.MaxBaseRadius, actorType);
                case RuleConstants.DefensiveBuildingPlacementValues.DISTRIBUTED_TO_IMPORTANT_STRUCTURES:
                    return FindBuildableLocationForImportantStructure(actorType);
                case RuleConstants.DefensiveBuildingPlacementValues.RANDOM:
                default:
                    return FindRandomBuildableLocation(baseCenter, 0, info.MaxBaseRadius, actorType);
            }
        }

        [Desc("Attempts to find a location that is nearest to a designated 'important structure'.")]
        private CPos FindBuildableLocationForImportantStructure(string actorType)
        {
            var importantOwnedActors = world.Actors.Where(a => a.Owner == selfPlayer && RuleConstants.DefensiveBuildingPlacementValues.IMPORTANT_STRUCTURES.Contains(a.Info.Name));
            // TODO: Choose at random here?
            var chosenActor = importantOwnedActors.Random(Random);

            return FindFirstBuildableLocation(chosenActor.Location, 0, info.MaxBaseRadius, actorType);
        }

        [Desc("Attempts to find a location to place a defensive structure, based on the DefensiveBuildingPlacement rule.")]
        private CPos FindNormalBuildingPlacement(StrategicWorldState state, CPos baseCenter, string actorType)
        {
            switch (info.NormalBuildingPlacement) {
                case RuleConstants.NormalBuildingPlacementValues.FARTHEST_FROM_ENEMY_LOCATIONS:
                    return FindBuildableLocationAwayFromEnemies(state, baseCenter, actorType);
                case RuleConstants.NormalBuildingPlacementValues.RANDOM:
                default:
                    return FindRandomBuildableLocation(baseCenter, 0, info.MaxBaseRadius, actorType);
            }
        }

        private CPos FindBuildableLocationAwayFromEnemies(StrategicWorldState state, CPos baseCenter, string actorType)
        {
            var bestEnemyLocation = state.EnemyInfoList.First().GetBestAvailableEnemyLocation(state, selfPlayer);
            var opposite = GeometryUtils.OppositeCornerOfNearestCorner(world.Map, bestEnemyLocation);
            var moveFromBaseToOpposite = GeometryUtils.MoveTowards(state.SelfIntialBaseLocation, opposite, info.MaxBaseRadius, world.Map);
            return FindFirstBuildableLocation(moveFromBaseToOpposite, 0, info.MaxBaseRadius, actorType);
        }
    }
}
