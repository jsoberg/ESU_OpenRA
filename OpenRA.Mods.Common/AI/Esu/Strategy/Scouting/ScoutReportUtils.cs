using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public static class ScoutReportUtils
    {

        public static List<Actor> EnemyActorsInWorld(StrategicWorldState state, Player selfPlayer)
        {
            return new List<Actor>(state.World.Actors.Where(a => a.Owner != selfPlayer && state.EnemyInfoList.Any(e => e.EnemyName == a.Owner.InternalName)
                && a.OccupiesSpace != null));
        }

        public static ScoutReportInfoBuilder BuildResponseInformationForActor(StrategicWorldState state, EsuAIInfo info, Actor actor, IEnumerable<Actor> enemyActors)
        {
            Rect visibileRect = VisibilityBounds.GetCurrentVisibilityRectForActor(actor);

            // Visible actors whose owners are in the enemy info list.
            var visibileEnemyItems = enemyActors.Where(a => visibileRect.ContainsPosition(a.CenterPosition));

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

                if (enemy.TraitOrDefault<Harvester>() != null) {
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
