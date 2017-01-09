using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class ScoutTargetLocationPool
    {
        private const int DistanceToWanderTowardEnemy = 8;

        private readonly Player SelfPlayer;
        private readonly Queue<CPos> AvailablePositions;
        private readonly MersenneTwister Random;

        private bool IsInitialized;

        public ScoutTargetLocationPool(Player selfPlayer)
        {
            this.SelfPlayer = selfPlayer;
            this.AvailablePositions = new Queue<CPos>();
            this.Random = new MersenneTwister();
        }

        public CPos GetAvailableTargetLocation(StrategicWorldState state, Actor scoutActor)
        {
            if (!IsInitialized) {
                InitializeTargetLocations(state);
                IsInitialized = true;
            }
            return GetNextTargetLocation(state, scoutActor);
        }

        // Initialize predicted enemy location queue, from most important to least.
        private void InitializeTargetLocations(StrategicWorldState state)
        {
            AddPredictedEnemyLocations(state);
            AddMapCorners(state.World);
        }

        private void AddPredictedEnemyLocations(StrategicWorldState state)
        {
            foreach (EnemyInfo enemy in state.EnemyInfoList) {
                // TODO: This is "2-player centric", in that it's predicting the same location for every enemy player. 
                // This works fine for one enemy, but once we begin facing more than that we'll need a better method.
                var predictedEnemyLocations = enemy.GetPredictedEnemyLocations(state, SelfPlayer);
                foreach (CPos location in predictedEnemyLocations) {
                    AvailablePositions.Enqueue(location);
                }
            }
        }

        private void AddMapCorners(World world)
        {
            var corners = GeometryUtils.GetMapCorners(world.Map);
            foreach (CPos corner in corners) {
                AvailablePositions.Enqueue(corner);
            }
        }

        private CPos GetNextTargetLocation(StrategicWorldState state, Actor scoutActor)
        {
            // If we can see an enemy actor, wander toward it.
            CPos wanderLocation = GetLocationForViewedEnemy(state, scoutActor);
            if (wanderLocation != CPos.Invalid) {
                return wanderLocation;
            }

            if (AvailablePositions.Count > 0) {
                return AvailablePositions.Dequeue();
            }
            return GetFoundEnemyLocationOrRandom(state, scoutActor);
        }

        private CPos GetLocationForViewedEnemy(StrategicWorldState state, Actor scoutActor)
        {
            Rect visibility = VisibilityBounds.GetCurrentVisibilityRectForActor(scoutActor);
            var visibleEnemyItems = state.World.Actors.Where(a => a.Owner != scoutActor.Owner && state.EnemyInfoList.Any(e => e.EnemyName == a.Owner.InternalName)
                && a.OccupiesSpace != null && visibility.ContainsPosition(a.CenterPosition));
            if (visibleEnemyItems == null || visibleEnemyItems.Count() == 0) {
                return CPos.Invalid;
            }

            // Move toward harvesters first if we see one.
            var harvester = visibleEnemyItems.FirstOrDefault(a => a.Info.Name == EsuAIConstants.Vehicles.HARVESTER);
            if (harvester != null) {
                return GeometryUtils.MoveTowards(scoutActor.Location, harvester.Location, DistanceToWanderTowardEnemy, state.World.Map);
            }

            // Move next toward buildings.
            var building = visibleEnemyItems.FirstOrDefault(a => EsuAIUtils.IsActorOfType(state.World, a, EsuAIConstants.ProductionCategories.BUILDING));
            if (building != null) {
                return GeometryUtils.MoveTowards(scoutActor.Location, building.Location, DistanceToWanderTowardEnemy, state.World.Map);
            }

            // Lastly, move toward units.
            var unit = visibleEnemyItems.FirstOrDefault(a => EsuAIUtils.IsActorOfType(state.World, a, EsuAIConstants.ProductionCategories.INFANTRY) 
                || EsuAIUtils.IsActorOfType(state.World, a, EsuAIConstants.ProductionCategories.VEHICLE));
            if (unit != null) {
                return GeometryUtils.MoveTowards(scoutActor.Location, unit.Location, DistanceToWanderTowardEnemy, state.World.Map);
            }

            return CPos.Invalid;
        }

        private CPos GetFoundEnemyLocationOrRandom(StrategicWorldState state, Actor scoutActor)
        {
            foreach (EnemyInfo enemy in state.EnemyInfoList) {
                if (enemy.FoundEnemyLocation != CPos.Invalid) {
                    return enemy.FoundEnemyLocation;
                }
            }
            return state.World.Map.AllCells.Where(c => scoutActor.Trait<Mobile>().CanMoveFreelyInto(c)).Random(Random);
        }
    }
}
