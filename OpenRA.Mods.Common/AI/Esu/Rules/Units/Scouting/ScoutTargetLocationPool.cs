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
            return GetNextAvailableTargetLocation(state, scoutActor);
        }

        // Initialize predicted enemy location queue, from most important to least.
        private void InitializeTargetLocations(StrategicWorldState state)
        {
            AddPredictedEnemyLocations(state);
            AddMapCorners(state.World);
        }

        private CPos GetNextAvailableTargetLocation(StrategicWorldState state, Actor scoutActor)
        {
            if (AvailablePositions.Count > 0) {
                return AvailablePositions.Dequeue();
            }
            return state.World.Map.AllCells.Where(c => scoutActor.Trait<Mobile>().CanMoveFreelyInto(c)).Random(Random);
        }

        private void AddPredictedEnemyLocations(StrategicWorldState state)
        {
            foreach (EnemyInfo enemy in state.EnemyInfoList) {
                // TODO: This is "2-player centric", in that it's predicting the same location for every enemy player. 
                // This works fine for one enemy, but once we begin facing more than that we'll need a better method.
                var predictedEnemyLocation = enemy.GetPredictedEnemyLocation(state, SelfPlayer);
                AvailablePositions.Enqueue(predictedEnemyLocation);
            }
        }

        private void AddMapCorners(World world)
        {
            var corners = GeometryUtils.GetMapCorners(world.Map);
            foreach (CPos corner in corners) {
                AvailablePositions.Enqueue(corner);
            }
        }
    }
}
