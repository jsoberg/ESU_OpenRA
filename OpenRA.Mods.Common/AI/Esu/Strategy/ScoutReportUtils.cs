using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public static class ScoutReportUtils
    {
        public static ResponseRecommendation.Builder BuildResponseInformationForActor(World world, EsuAIInfo info, Actor actor)
        {
            Rect visibileRect = VisibilityBounds.GetCurrentVisibilityRectForActor(actor);
            var visibileEnemyItems = world.Actors.Where(a => a.Owner != actor.Owner && visibileRect.ContainsPosition(a.CenterPosition));
            if (visibileEnemyItems == null || visibileEnemyItems.Count() == 0) {
                return null;
            }

            ResponseRecommendation.Builder builder = new ResponseRecommendation.Builder(info);
            foreach (Actor enemy in visibileEnemyItems) {
                AddInformationForEnemyActor(world, builder, enemy);
            }
            return builder;
        }

        private static void AddInformationForEnemyActor(World world, ResponseRecommendation.Builder builder, Actor enemy)
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
                if (IsEnemyActorOfType(world, enemy, EsuAIConstants.ProductionCategories.DEFENSE))
                {
                    string name = enemy.Info.Name;

                    if (EsuAIConstants.Defense.IsAntiInfantry(name))
                    {
                        builder.AddAntiInfantryDefensiveBuilding();
                    }

                    if (EsuAIConstants.Defense.IsAntiVehicle(name))
                    {
                        builder.AddAntiVehicleDefensiveBuilding();
                    }

                    if (EsuAIConstants.Defense.IsAntiAir(name))
                    {
                        builder.AddAntiAirDefensiveBuilding();
                    }

                    builder.AddOtherDefensiveBuilding();
                    return;
                }
            }

            // Units.
            {
                if (IsEnemyActorOfType(world, enemy, EsuAIConstants.ProductionCategories.INFANTRY))
                {
                    builder.AddInfantry();
                    return;
                }

                if (IsEnemyActorOfType(world, enemy, EsuAIConstants.ProductionCategories.VEHICLE))
                {
                    builder.AddVehicle();
                    return;
                }

                if (IsEnemyActorOfType(world, enemy, EsuAIConstants.ProductionCategories.AIRCRAFT))
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

        private static bool IsEnemyActorOfType(World world, Actor enemy, string type)
        {
            IEnumerable<ProductionQueue> queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, enemy.Owner, type);
            foreach (ProductionQueue queue in queues)
            {
                var producables = queue.AllItems();
                foreach (ActorInfo producable in producables)
                {
                    if (enemy.Info.Name == producable.Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
