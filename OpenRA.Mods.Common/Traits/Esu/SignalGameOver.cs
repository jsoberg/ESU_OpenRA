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
            Console.WriteLine("Game Complete!");
            Log.Write("order_manager", "Game Complete!");
        }
    }
}
