using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Defense
{
    public class BaseLethalityMetric
    {
        /// <summary>
        ///  Map describing vulnerable actors and their corresponding lethality ratings.
        /// </summary>
        public readonly Dictionary<Actor, int> VulnerableActorToLethalityMap;
        /// <summary>
        ///  Map describing offensive actors and the amount of lethality that they can cover.
        /// </summary>
        public readonly Dictionary<Actor, int> OffensiveActorToLethalityMap;

        public BaseLethalityMetric(World world, Player selfPlayer)
        {
            this.VulnerableActorToLethalityMap = BuildVulnerableActorMapForPlayer(world, selfPlayer);
            this.OffensiveActorToLethalityMap = BuildOffensiveActorMapForPlayer(world, selfPlayer);
        }

        private Dictionary<Actor, int> BuildVulnerableActorMapForPlayer(World world, Player selfPlayer)
        {
            //TODO stub
            return null;
        }

        private Dictionary<Actor, int> BuildOffensiveActorMapForPlayer(World world, Player selfPlayer)
        {
            //TODO stub
            return null;
        }
    }
}