using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public static class ScoutReportUtils
    {

        public static List<Actor> EnemyActorsInWorld(StrategicWorldState state, Player selfPlayer, List<Actor> reuse)
        {
            reuse.Clear();
            reuse.AddRange(state.World.Actors.Where(a => a.Owner != selfPlayer && state.EnemyInfoList.Any(e => e.EnemyName == a.Owner.InternalName)
                && a.OccupiesSpace != null));
            return reuse;
        }

        public static ScoutReportInfoBuilder BuildResponseInformationForActor(StrategicWorldState state, EsuAIInfo info, Actor actor, IEnumerable<Actor> enemyActors)
        {
            return BuildResponseInformationForActor(state, info, actor, enemyActors, null);
        }

        public static ScoutReportInfoBuilder BuildResponseInformationForActor(StrategicWorldState state, EsuAIInfo info, Actor actor, IEnumerable<Actor> enemyActors, Actor killingActor)
        {
            IEnumerable<Actor> visibileEnemyItems = null;
            if (!actor.IsDead) {
                // Actor isn't dead, so look for actors whose owners are in the enemy info list.
                Rect visibileRect = VisibilityBounds.GetCurrentVisibilityRectForActor(actor);
                visibileEnemyItems = enemyActors.Where(a => visibileRect.ContainsPosition(a.CenterPosition));
            } else if (killingActor != null) {
                // Actor is dead, so just issue report for killer.
                var killingActorList = new List<Actor>();
                killingActorList.Add(killingActor);
                visibileEnemyItems = killingActorList;
            }

            if (visibileEnemyItems == null || visibileEnemyItems.Count() == 0) {
                return null;
            }
            ScoutReportInfoBuilder builder = new ScoutReportInfoBuilder(info);
            foreach (Actor enemy in visibileEnemyItems) {
                AddInformationForEnemyActor(state.World, builder, enemy);
            }
            return builder;
        }

        private static void AddInformationForEnemyActor(World world, ScoutReportInfoBuilder builder, Actor enemy)
        {
            // Power plants.
            {
                if (enemy.Info.Name == EsuAIConstants.Buildings.POWER_PLANT)
                {
                    builder.AddPowerPlant();
                    return;
                }

                if (enemy.Info.Name == EsuAIConstants.Buildings.ADVANCED_POWER_PLANT)
                {
                    builder.AddAdvancedPowerPlant();
                    return;
                }
            }

            // Defensive structures.
            {
                if (EsuAIUtils.IsActorOfType(world, enemy, EsuAIConstants.ProductionCategories.DEFENSE))
                {
                    string name = enemy.Info.Name;
                    builder.AddDefensiveBuilding(name);
                    return;
                }
            }

            // Units.
            {
                if (EsuAIUtils.IsActorOfType(world, enemy, EsuAIConstants.ProductionCategories.INFANTRY))
                {
                    builder.AddInfantry();
                    return;
                }

                if (!enemy.IsDead && enemy.TraitOrDefault<Harvester>() != null) {
                    builder.AddHarvester();
                    return;
                } else if (EsuAIUtils.IsActorOfType(world, enemy, EsuAIConstants.ProductionCategories.VEHICLE)) {
                    builder.AddVehicle();
                    return;
                }

                if (EsuAIUtils.IsActorOfType(world, enemy, EsuAIConstants.ProductionCategories.AIRCRAFT))
                {
                    builder.AddAircraft();
                    return;
                }
            }

            // Ore refineries.
            {
                if (enemy.Info.Name == EsuAIConstants.Buildings.ORE_REFINERY)
                {
                    builder.AddOreRefinery();
                    return;
                }
            }

            // Everything else.
            {
                builder.AddGenericBuilding();
            }
        }
    }
}
