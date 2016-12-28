using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Esu
{
    public class SignalFitnessLogInfo : ITraitInfo
    {
        public const string DEFAULT_FITNESS_LOG_NAME = "end_game_fitness";

        public object Create(ActorInitializer init)
        {
            Log.AddChannel(DEFAULT_FITNESS_LOG_NAME, DEFAULT_FITNESS_LOG_NAME + ".log");
            return new SignalFitnessLog(init.World);
        }
    }

    /** A simple callback to tell us when the game is over, or we need to log a periodic fitness value. */
    public class SignalFitnessLog : IGameOver, ITick
    {
        private const string FORMAT_STRING = "{0,-30} | {1,-30} | {2,-30} | {3,-30}\n";

        private readonly World world;

        public SignalFitnessLog(World world)
        {
            this.world = world;
        }

        void ITick.Tick(Actor self)
        {
            if ((world.GetCurrentLocalTickCount() % world.GetFitnessLogTickIncrement()) == 0) {
                PrintPlayerFitnessInformation();
            }
        }

        public void GameOver(World world)
        {
            Console.WriteLine("Game Complete!");
            PrintPlayerFitnessInformation();

            // Kill process.
            System.Environment.Exit(0);
        }

        private void PrintPlayerFitnessInformation()
        {
            PrintToConsoleAndLog(world, String.Format(FORMAT_STRING, "PLAYER NAME", "KILL COST", "DEATH COST", "TICK COUNT"));
           
            foreach (var p in world.Players.Where(a => !a.NonCombatant))
            {
                var stats = p.PlayerActor.TraitOrDefault<PlayerStatistics>();
                if (stats == null) {
                    continue;
                }

                PrintToConsoleAndLog(world, String.Format(FORMAT_STRING, p.PlayerName, stats.KillsCost, stats.DeathsCost, world.GetCurrentLocalTickCount()));
            }
        }

        private void PrintToConsoleAndLog(World world, string message)
        {
            Console.WriteLine(message);
            Log.Write(SignalFitnessLogInfo.DEFAULT_FITNESS_LOG_NAME, message);
        }
    }
}