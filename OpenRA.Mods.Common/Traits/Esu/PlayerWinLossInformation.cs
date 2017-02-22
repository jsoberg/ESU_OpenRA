using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Esu
{
    public class PlayerWinLossInformationInfo : ITraitInfo
    {
        public object Create(ActorInitializer init)
        {
            return new PlayerWinLossInformation();
        }
    }

    public class PlayerWinLossInformation : INotifyObjectivesUpdated
    {
        public static string WinningPlayer;

        void INotifyObjectivesUpdated.OnPlayerWon(Player winner)
        {
            WinningPlayer = winner.PlayerName;
        }

        void INotifyObjectivesUpdated.OnPlayerLost(Player loser)
        {
            /* Unused. */
        }

        void INotifyObjectivesUpdated.OnObjectiveAdded(Player player, int objectiveID)
        {
            /* Unused. */
        }

        void INotifyObjectivesUpdated.OnObjectiveCompleted(Player player, int objectiveID)
        {
            /* Unused. */
        }

        void INotifyObjectivesUpdated.OnObjectiveFailed(Player player, int objectiveID)
        {
            /* Unused. */
        }
    }
}
