using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;
using OpenRA.Mods.Common.AI.Esu.Rules.Units;

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

        public readonly CompiledUnitDamageStatisticsLoader UnitStatsLoader;
        public readonly List<ScoutActor> CurrentScouts;

        public ScoutReportLocationGrid ScoutReportGrid;
        public CPos SelfIntialBaseLocation;
        public World World;
		public Player SelfPlayer;
        public EsuAIInfo Info;

        public bool CheckNewDefensiveStructureFlag;
        public bool CheckAttackStrengthPredictionFlag;
        public ActiveAttackController ActiveAttackController;

        // Actor caches
        private readonly List<Actor> InternalOffensiveActorsCache;
        public readonly ReadOnlyCollection<Actor> OffensiveActorsCache;

        private readonly List<Actor> InternalDefensiveStructureCache;
        public readonly ReadOnlyCollection<Actor> DefensiveStructureCache;

        // Resource cache
        public readonly Dictionary<ResourceTile, HashSet<CPos>> ResourceCache;

        public StrategicWorldState()
        {
            this.EnemyInfoList = new List<EnemyInfo>();
            this.RequestedBuildingQueue = new Queue<string>();

            this.UnitStatsLoader = new CompiledUnitDamageStatisticsLoader();
            this.CurrentScouts = new List<ScoutActor>();

            this.InternalOffensiveActorsCache = new List<Actor>();
            this.OffensiveActorsCache = InternalOffensiveActorsCache.AsReadOnly();
            this.InternalDefensiveStructureCache = new List<Actor>();
            this.DefensiveStructureCache = InternalDefensiveStructureCache.AsReadOnly();

            this.ResourceCache = new Dictionary<ResourceTile, HashSet<CPos>>();
        }

        public void Initalize(World world, EsuAIInfo info, Player selfPlayer)
        {
            this.World = world;
            this.Info = info;
            this.SelfPlayer = selfPlayer;

            this.ScoutReportGrid = new ScoutReportLocationGrid(this);
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

        public void UnitProduced(Actor producer, Actor produced)
        {
            if (produced.Owner == SelfPlayer && produced.Info.Name != "harv" && !EsuAIUtils.IsActorOfType(World, produced, EsuAIConstants.ProductionCategories.BUILDING)
                && !EsuAIUtils.IsActorOfType(World, produced, EsuAIConstants.ProductionCategories.DEFENSE) && !CurrentScouts.Any(a => a.Actor == produced))
            {
                InternalOffensiveActorsCache.Add(produced);
            }
        }

        // ============================================
        // Tick Updates
        // ============================================

        public void Tick()
        {
            if (CheckNewDefensiveStructureFlag)
            {
                var found = FindNewDefensiveStructures();
                if (found)
                {
                    CheckNewDefensiveStructureFlag = false;
                }
            }

            // Check for enemy info every 10 ticks.
            if (World.GetCurrentLocalTickCount() % 10 == 0)
            {
                VisibilityBounds visibility = VisibilityBounds.CurrentVisibleAreaForPlayer(World, SelfPlayer);
                foreach (EnemyInfo info in EnemyInfoList)
                {
                    TryFindEnemy(info, visibility);
                }
            }

            ScoutReportGrid.PerformUpdates(World);
            RemoveDeadActorsFromCaches();
        }

        /** @return true if new defensive structures were found, false otherwise. */
        private bool FindNewDefensiveStructures()
        {
            var defensiveStructures = World.Actors.Where(a => a.Owner == SelfPlayer && EsuAIUtils.IsActorOfType(World, a, EsuAIConstants.ProductionCategories.DEFENSE));
            foreach (Actor actor in defensiveStructures) {
                if (!InternalDefensiveStructureCache.Contains(actor)) {
                    InternalDefensiveStructureCache.Clear();
                    InternalDefensiveStructureCache.AddRange(defensiveStructures);
                    return true;
                }
            }
            return false;
        }

        private void TryFindEnemy(EnemyInfo info, VisibilityBounds visibility)
        {
            if (info.FirstFoundEnemyStructureLocation == CPos.Invalid)
            {
                var enemyStructures = World.Actors.Where(a => a.Owner.InternalName == info.EnemyName 
                    && (EsuAIUtils.IsActorOfType(World, a, EsuAIConstants.ProductionCategories.BUILDING) 
                    || EsuAIUtils.IsActorOfType(World, a, EsuAIConstants.ProductionCategories.DEFENSE)));

                foreach (Actor structure in enemyStructures) {
                    if (visibility.ContainsPosition(structure.CenterPosition)) {
                        info.FirstFoundEnemyStructureLocation = structure.Location;
                        break;
                    }
                }
            }

            var enemyConstructionYard = World.Actors.FirstOrDefault(a => a.Owner.InternalName == info.EnemyName && a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD);
            if (enemyConstructionYard == null) {
                return;
            }

            // If we're just finding this enemy's location now, set it for later.
            if (info.FoundEnemyLocation == CPos.Invalid && visibility.ContainsPosition(enemyConstructionYard.CenterPosition)) {
                info.FoundEnemyLocation = enemyConstructionYard.Location;
            }
        }

        public void AddScoutReportInformation(Actor scoutActor, ScoutReportInfoBuilder infoBuilder)
        {
            ResponseRecommendation recommendation = new ResponseRecommendation(infoBuilder, UnitStatsLoader.GetUnitDamageStatistics());
            // 0/0 reports tell us nothing and should be ignored.
            if (recommendation.RewardValue == 0 && recommendation.RiskValue == 0) {
                return;
            }

            ScoutReport report = new ScoutReport(recommendation, scoutActor.Location, scoutActor.CenterPosition, World);
            ScoutReportGrid.QueueScoutReport(report);
        }

        public CPos GetClosestEnemyLocation(CPos current)
        {
            CPos closest = CPos.Invalid;
            double minDistance = double.MaxValue;

            foreach (EnemyInfo enemy in EnemyInfoList) {
                CPos enemyLoc = enemy.GetBestAvailableEnemyLocation(this, SelfPlayer);
                double dist = GeometryUtils.EuclideanDistance(current, enemyLoc);

                if (dist < minDistance) {
                    minDistance = dist;
                    closest = enemyLoc;
                }                
            }
            return closest;
        }

        private void RemoveDeadActorsFromCaches()
        {
            InternalOffensiveActorsCache.RemoveAll(a => a.IsDead);
            InternalDefensiveStructureCache.RemoveAll(a => a.IsDead);
        }
    }

    /// <summary>
    ///  This class is a data strucure which holds information about a given enemy.
    /// </summary>
    public class EnemyInfo
    {
        public readonly string EnemyName;
        public CPos FoundEnemyLocation { get; set; }
        public CPos FirstFoundEnemyStructureLocation { get; set; }

        private IEnumerable<CPos> PredictedEnemyLocationsCache;

        public EnemyInfo(string name, World world, Player selfPlayer)
        {
            this.EnemyName = name;

            // Default
            FoundEnemyLocation = CPos.Invalid;
            FirstFoundEnemyStructureLocation = CPos.Invalid;
        }
        
        public CPos GetBestAvailableEnemyLocation(StrategicWorldState state, Player selfPlayer)
        {
            return (FoundEnemyLocation == CPos.Invalid) ? GetPredictedEnemyLocations(state, selfPlayer).First() : FoundEnemyLocation;
        }

        public IEnumerable<CPos> GetPredictedEnemyLocations(StrategicWorldState state, Player selfPlayer)
        {
            // TODO: This is "2-player centric", in that it's predicting the same location for every enemy player. 
            // This works fine for one enemy, but once we begin facing more than that we'll need a better method.
            if (PredictedEnemyLocationsCache == null)
            {
                try {
                    PredictedEnemyLocationsCache = EsuAIUtils.PossibleEnemyLocationsForPlayer(state.World, selfPlayer);
                } catch (NoConstructionYardException) {
                }
            }
            return PredictedEnemyLocationsCache == null ? new List<CPos>() { CPos.Invalid } : PredictedEnemyLocationsCache;
        }
    }
}
