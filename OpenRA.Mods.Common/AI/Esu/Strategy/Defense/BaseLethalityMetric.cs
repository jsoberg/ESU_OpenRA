using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Rules;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;

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
            var vulnerableItems = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead 
                && (!a.Info.HasTraitInfo<ArmamentInfo>() && !a.Info.HasTraitInfo<AttackGarrisonedInfo>()) && a.TraitOrDefault<Health>() != null);

            Dictionary<Actor, int> map = new Dictionary<Actor, int>();
            foreach (Actor item in vulnerableItems) {
                // TODO do we want current HP, or max HP? Also consider cost, test and see what's most useful.
                map.Add(item, item.Trait<Health>().HP);
            }
            return map;
        }

        private Dictionary<Actor, int> BuildOffensiveActorMapForPlayer(World world, Player selfPlayer)
        {
            var offensiveItems = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead 
                && (a.Info.HasTraitInfo<ArmamentInfo>() || a.Info.HasTraitInfo<AttackGarrisonedInfo>()) && a.TraitOrDefault<Health>() != null);

            Dictionary<Actor, int> map = new Dictionary<Actor, int>();
            foreach (Actor item in offensiveItems) {
                // TODO find actual lethality metric to use (Maybe something in item.Trait<Armament>().Weapon?)
                map.Add(item, item.Trait<Health>().HP);
            }
            return map;
        }

        /// <summary>
        ///  Provides defensive coverage of base, without taking into account actor placement.
        /// </summary>
        public DefenseCoverage CurrentDefenseCoverage_Simple(double desiredDefensePercentage, List<ActiveAttack> currentAttacks)
        {
            List<Actor> necessaryActors = new List<Actor>();
            int currentLethalityNeeded = GetLethalityCoverageRequiredForVulnerableUnits(desiredDefensePercentage);
            Dictionary<Actor, int> offenseClone = new Dictionary<Actor, int>(OffensiveActorToLethalityMap);
            // Remove any actors currently in an attack.
            foreach (ActiveAttack attack in currentAttacks) {
                foreach (Actor attackActor in attack.AttackTroops) {
                    offenseClone.Remove(attackActor);
                }
            }

            while (currentLethalityNeeded > 0 && offenseClone.Count > 0)
            {
                KeyValuePair<Actor, int> newUnit = GetNewOffensiveUnitAndRemove(offenseClone);
                necessaryActors.Add(newUnit.Key);
                currentLethalityNeeded -= newUnit.Value;
            }

            return new DefenseCoverage(currentLethalityNeeded, necessaryActors);
        }

        private int GetLethalityCoverageRequiredForVulnerableUnits(double desiredDefensePercentage)
        {
            int lethalityNeeded = 0;
            foreach (int entry in VulnerableActorToLethalityMap.Values) {
                lethalityNeeded += entry;
            }

            return (int) Math.Round(lethalityNeeded * desiredDefensePercentage);
        }

        private KeyValuePair<Actor, int> GetNewOffensiveUnitAndRemove(Dictionary<Actor, int> offensiveUnits)
        {
            // Getting first available unit for now.
            KeyValuePair<Actor, int> newUnit = offensiveUnits.ElementAt(0);
            offensiveUnits.Remove(newUnit.Key);
            return newUnit;
        }

        public class DefenseCoverage
        {
            public bool IsFullyCovered { get { return (AdditionalLethalityNeededToDefend > 0); } }
            public int AdditionalLethalityNeededToDefend;
            public readonly List<Actor> ActorsNecessaryForDefense;

            public DefenseCoverage(int additionalLethalityNeededToDefend, List<Actor> necessaryActors)
            {
                this.AdditionalLethalityNeededToDefend = additionalLethalityNeededToDefend;
                this.ActorsNecessaryForDefense = necessaryActors;
            }
        }
    }
}