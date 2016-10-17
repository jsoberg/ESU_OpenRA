using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;

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
            var vulnerableItems = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && (a.Trait<Armament>() == null && a.Trait<AttackGarrisoned>() == null));

            Dictionary<Actor, int> map = new Dictionary<Actor, int>();
            foreach (Actor item in vulnerableItems) {
                // TODO do we want current HP, or max HP? Also consider cost, test and see what's most useful.
                map.Add(item, item.Trait<Health>().HP);
            }
            return map;
        }

        private Dictionary<Actor, int> BuildOffensiveActorMapForPlayer(World world, Player selfPlayer)
        {
            var offensiveItems = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && (a.Trait<Armament>() != null || a.Trait<AttackGarrisoned>() != null));

            Dictionary<Actor, int> map = new Dictionary<Actor, int>();
            foreach (Actor item in offensiveItems) {
                // TODO find actual lethality metric to use (Maybe something in item.Trait<Armament>().Weapon?)
                map.Add(item, item.Trait<Health>().HP);
            }
            return map;
        }
    }
}