using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu;

namespace OpenRA.Mods.Common.AI.Esu
{
    /// <summary>
    ///  This class is the strategic world state that is used by the EsuAi to make various decisions.
    /// </summary>
    public class StrategicWorldState
    {
        public readonly List<EnemyInfo> EnemyInfoList;

        public World world;
        public Player selfPlayer;
        public bool IsInitialized { get; private set; }

        public StrategicWorldState()
        {
            this.EnemyInfoList = new List<EnemyInfo>();
        }

        public void Initalize(World world, Player selfPlayer)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;

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
            VisibilityBounds visibility = VisibilityBounds.CurrentVisibleAreaForPlayer(world, selfPlayer);
            foreach (EnemyInfo info in EnemyInfoList) {
                TryFindEnemyConstructionYard(info, visibility);
            }
        }

        private void TryFindEnemyConstructionYard(EnemyInfo info, VisibilityBounds visibility)
        {
            var enemyConstructionYard = world.Actors.FirstOrDefault(a => a.Owner.InternalName == info.EnemyName && a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD);
            if (enemyConstructionYard == null) {
                return;
            }

            // If we're just finding this enemy's location now, set it for later.
            if (info.FoundEnemyLocation != CPos.Invalid && visibility.ContainsPosition(enemyConstructionYard.CenterPosition)) {
                info.FoundEnemyLocation = enemyConstructionYard.Location;
            }
        }
    }

    /// <summary>
    ///  This class is a data strucure which holds information about a given enemy.
    /// </summary>
    public class EnemyInfo
    {
        public readonly string EnemyName;
        public readonly CPos PredictedEnemyLocation;

        public bool IsScouting { get; set; }
        public CPos FoundEnemyLocation { get; set; }

        public EnemyInfo(string name, World world, Player selfPlayer)
        {
            this.EnemyName = name;
            // TODO: This is "2-player centric", in that it's predicting the same location for every enemy player. 
            // This works fine for one enemy, but once we begin facing more than that we'll need a better method.
            this.PredictedEnemyLocation = EsuAIUtils.OppositeBaseLocationOfPlayer(world, selfPlayer);

            // Default
            FoundEnemyLocation = CPos.Invalid;
        }
    }
}
