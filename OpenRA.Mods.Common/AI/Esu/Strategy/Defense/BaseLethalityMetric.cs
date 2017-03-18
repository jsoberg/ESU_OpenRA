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

        public BaseLethalityMetric(StrategicWorldState state, Player selfPlayer)
        {
            this.VulnerableActorToLethalityMap = BuildVulnerableActorMapForPlayer(state, selfPlayer);
            this.OffensiveActorToLethalityMap = BuildOffensiveActorMapForPlayer(state, selfPlayer);
        }

        private Dictionary<Actor, int> BuildVulnerableActorMapForPlayer(StrategicWorldState state, Player selfPlayer)
        {
            var vulnerableItems = state.World.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead 
                && (state.ProducedBuildingsCache.Contains(a.Info.Name) || a.Info.Name == EsuAIConstants.Vehicles.HARVESTER)
                && a.TraitOrDefault<Health>() != null);

            Dictionary<Actor, int> map = new Dictionary<Actor, int>();
            foreach (Actor actor in vulnerableItems) {
                // TODO do we want current HP, or max HP? Also consider cost, test and see what's most useful.
                map.Add(actor, LethalityForActor(actor));
            }
            return map;
        }

        private Dictionary<Actor, int> BuildOffensiveActorMapForPlayer(StrategicWorldState state, Player selfPlayer)
        {
            Dictionary<Actor, int> map = new Dictionary<Actor, int>();
            foreach (Actor actor in state.OffensiveActorsCache) {
                // TODO find actual lethality metric to use (Maybe something in item.Trait<Armament>().Weapon?)
                map.Add(actor, LethalityForActor(actor));
            }
            return map;
        }

        private int LethalityForActor(Actor actor)
        {
            return actor.Trait<Health>().HP;
        }

        /// <summary>
        ///  Provides defensive coverage of base, without taking into account actor placement.
        /// </summary>
        public DefenseCoverage CurrentDefenseCoverage_Simple(StrategicWorldState state, double desiredDefensePercentage, IEnumerable<ActiveAttack> currentAttacks)
        {
            List<Actor> necessaryActors = new List<Actor>();
            int currentLethalityNeeded = GetLethalityCoverageRequiredForVulnerableUnits(state, desiredDefensePercentage);
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

        private int GetLethalityCoverageRequiredForVulnerableUnits(StrategicWorldState state, double desiredDefensePercentage)
        {
            int lethalityNeeded = 0;
            foreach (int entry in VulnerableActorToLethalityMap.Values) {
                lethalityNeeded += entry;
            }

            // Account for static defensive coverage at base.
            foreach (Actor defender in state.DefensiveStructureCache) {
                lethalityNeeded -= LethalityForActor(defender);
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