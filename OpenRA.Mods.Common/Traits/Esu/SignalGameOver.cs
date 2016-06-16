using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Esu
{
    public class SignalGameOverInfo : ITraitInfo
    {
        public object Create(ActorInitializer init)
        {
            return new SignalGameOver();
        }
    }

    /** A simple callback to tell us when the game is over. */
    public class SignalGameOver : IGameOver
    {
        public void GameOver(World world)
        {
            PrintToConsoleAndLog("Game Complete!");
            PrintPlayerFitnessInformation(world);
        }

        private void PrintPlayerFitnessInformation(World world)
        {
            foreach (var p in world.Players.Where(a => !a.NonCombatant))
            {
                var stats = p.PlayerActor.TraitOrDefault<PlayerStatistics>();
                if (stats == null)
                {
                    continue;
                }

                var totalKills = stats.UnitsKilled + stats.BuildingsKilled;
                var totalDeaths = stats.UnitsDead + stats.BuildingsDead;

                PrintToConsoleAndLog("Player {0}: {1} kills, {2} deaths".F(p.PlayerName, totalKills, totalDeaths));
            }
        }

        private void PrintToConsoleAndLog(string message)
        {
            Console.WriteLine(message);
            Log.Write("order_manager", message);
        }
    }
}
