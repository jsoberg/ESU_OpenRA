using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    /// <summary>
    ///  This class is the strategic world state that is used by the EsuAi to make various decisions.
    /// </summary>
    public class StrategicWorldState
    {
        public bool IsInitialized { get; private set; }

        public readonly List<EnemyInfo> EnemyInfoList;
        // This queue will be periodically polled from the build ruleset.
        public readonly Queue<string> RequestedBuildingQueue;

        public ScoutReportLocationGrid ScoutReportGrid;
        public CPos SelfIntialBaseLocation;
        public World World;
		public Player SelfPlayer;

        public ActiveAttackController ActiveAttackController;

        public StrategicWorldState()
        {
            this.EnemyInfoList = new List<EnemyInfo>();
            this.RequestedBuildingQueue = new Queue<string>();
        }

        public void Initalize(World world, Player selfPlayer)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;

            this.ScoutReportGrid = new ScoutReportLocationGrid(world);
            this.ActiveAttackController = new ActiveAttackController(world);

            // Cache our own location for now.
            var selfYard = world.Actors.Where(a => a.Owner == selfPlayer &&
                a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD).FirstOrDefault();
            if (selfYard == null) {
                // Try again until we have a contruction yard.
                return;
            }
            SelfIntialBaseLocation = selfYard.Location;

            var enemyPlayers = world.Players.Where(p => p != selfPlayer && !p.NonCombatant && p.IsBot);
            foreach (Player p in enemyPlayers) {

                try {
                    EnemyInfo enemy = new EnemyInfo(p.InternalName, world, selfPlayer);
                    EnemyInfoList.Add(enemy);
                } catch (NoConstructionYardException) {
                    // Try again until we have a contruction yard.
                    return;
                }
            }

            IsInitialized = true;
        }

        public void UpdateCurrentWorldState()
        {
            VisibilityBounds visibility = VisibilityBounds.CurrentVisibleAreaForPlayer(World, SelfPlayer);
            foreach (EnemyInfo info in EnemyInfoList) {
                TryFindEnemyConstructionYard(info, visibility);
            }

            ScoutReportGrid.PerformUpdates(World);
        }

        private void TryFindEnemyConstructionYard(EnemyInfo info, VisibilityBounds visibility)
        {
            var enemyConstructionYard = World.Actors.FirstOrDefault(a => a.Owner.InternalName == info.EnemyName && a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD);
            if (enemyConstructionYard == null) {
                return;
            }

            // If we're just finding this enemy's location now, set it for later.
            if (info.FoundEnemyLocation != CPos.Invalid && visibility.ContainsPosition(enemyConstructionYard.CenterPosition)) {
                info.FoundEnemyLocation = enemyConstructionYard.Location;
            }
        }

        public void AddScoutReportInformation(Actor scoutActor, ScoutReportInfoBuilder infoBuilder)
        {
            ResponseRecommendation recommendation = new ResponseRecommendation(infoBuilder);
            ScoutReport report = new ScoutReport(recommendation, scoutActor.CenterPosition, World);

            ScoutReportGrid.AddScoutReportForActor(scoutActor, report);
        }
    }

    /// <summary>
    ///  This class is a data strucure which holds information about a given enemy.
    /// </summary>
    public class EnemyInfo
    {
        public readonly string EnemyName;
        public CPos FoundEnemyLocation { get; set; }

        public EnemyInfo(string name, World world, Player selfPlayer)
        {
            this.EnemyName = name;

            // Default
            FoundEnemyLocation = CPos.Invalid;
        }
        
        public CPos GetBestAvailableEnemyLocation(StrategicWorldState state, Player selfPlayer)
        {
            return (FoundEnemyLocation == CPos.Invalid) ? GetPredictedEnemyLocations(state, selfPlayer).First() : FoundEnemyLocation;
        }

        public IEnumerable<CPos> GetPredictedEnemyLocations(StrategicWorldState state, Player selfPlayer)
        {
            // TODO: This is "2-player centric", in that it's predicting the same location for every enemy player. 
            // This works fine for one enemy, but once we begin facing more than that we'll need a better method.
            return EsuAIUtils.PossibleEnemyLocationsForPlayer(state.World, selfPlayer);
        }
    }
}
