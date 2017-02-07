using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Defense;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    /// <summary>
    ///  Trying to give a discrete description for the strength of any given predicted attack.
    /// </summary>
    public enum PredictedAttackStrength
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Overwhelming = 4
    };

    public class AttackStrengthPredictor
    {
        private readonly BaseLethalityMetric Metric;
        private readonly StrategicWorldState State;

        /** Minimum lethality before we'll consider an attack. */
        private int MinimumLethality = 400;
        /** Lethality step to consider our available lethality to be "on the next level"
         *  e.g. If the minimum lethality is 400 and the step is 100, a lethality of 600 would be considered 2 levels above minimum. */
        private int LethalityStep = 100;

        public AttackStrengthPredictor(BaseLethalityMetric metric, StrategicWorldState state)
        {
            this.Metric = metric;
            this.State = state;
        }

        public void SetLethalitySettings(int minimumLethality, int lethalityStep)
        {
            this.MinimumLethality = minimumLethality;
            this.LethalityStep = lethalityStep;
        }

        public PredictedAttackStrength PredictStrengthForAttack(int risk, int reward, IEnumerable<Actor> attackActors, CPos location)
        {
            BestScoutReportData data = State.ScoutReportGrid.GetBestScoutReportDataFromDatabase();

            // TODO: Add more factors into attack strength (enemy composition, etc).
            PredictedAttackStrength riskRewardStrength = AttackStrengthBasedOnRiskAndReward(risk, reward);
            PredictedAttackStrength lethalityStrength = AttackStrengthBasedOnAvailableLethality(attackActors);
            return (PredictedAttackStrength) (((int) riskRewardStrength + (int) lethalityStrength) / 2);
        }

        private PredictedAttackStrength AttackStrengthBasedOnAvailableLethality(IEnumerable<Actor> attackActors)
        {
            double basicAttackLethality = GetBasicAttackLethality(attackActors);

            if (basicAttackLethality < MinimumLethality)
            {
                return PredictedAttackStrength.None;
            }
            else if (basicAttackLethality < (MinimumLethality + LethalityStep))
            {
                return PredictedAttackStrength.Low;
            }
            else if (basicAttackLethality < (MinimumLethality + 2 * LethalityStep))
            {
                return PredictedAttackStrength.Medium;
            }
            else if (basicAttackLethality < (MinimumLethality + 3 * LethalityStep))
            {
                return PredictedAttackStrength.High;
            }

            return PredictedAttackStrength.Overwhelming;
        }

        private double GetBasicAttackLethality(IEnumerable<Actor> attackActors)
        {
            double lethality = 0;
            foreach (Actor actor in attackActors) {
                lethality += LethalityForActor(actor);
            }
            return lethality;
        }

        private double LethalityForActor(Actor actor)
        {
            double totalPossibleDamage = 0;
            double numDamageUnits = 0;

            var armaments = actor.TraitsImplementing<Armament>();
            foreach (Armament armament in armaments)
            {
                var warheads = armament.Weapon.Warheads;
                foreach (IWarhead warhead in warheads)
                {
                    if (warhead is DamageWarhead)
                    {
                        totalPossibleDamage += ((DamageWarhead)warhead).Damage;
                        numDamageUnits++;
                    }
                }
            }
            return (totalPossibleDamage / numDamageUnits);
        }

        private PredictedAttackStrength AttackStrengthBasedOnRiskAndReward(int risk, int reward)
        {
            BestScoutReportData data = State.ScoutReportGrid.GetBestScoutReportDataFromDatabase();
            if (data == null) {
                // Return a default of medium if we have no available data.
                return PredictedAttackStrength.Medium;
            }

            int rewardStep = (data.HighestReward - data.LowestReward) / 2;
            int riskStep = (data.HighestRisk - data.LowestRisk) / 2;
            if (risk < data.LowestRisk && reward > data.HighestReward)
            {
                return PredictedAttackStrength.Overwhelming;
            }
            else if (risk <= data.LowestRisk && reward >= data.HighestReward)
            {
                return PredictedAttackStrength.High;
            }
            else if (risk <= (data.LowestRisk + (riskStep / 2)) && reward >= (data.HighestReward - (rewardStep / 2)))
            {
                return PredictedAttackStrength.Medium;
            }
            else if (risk <= (data.LowestRisk + riskStep) && reward >= (data.HighestReward - rewardStep))
            {
                return PredictedAttackStrength.Low;
            }
            return PredictedAttackStrength.None;
        }
    }
}
