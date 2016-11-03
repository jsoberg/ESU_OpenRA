using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public static class AttackLogicUtils
    {
        /// <summary>
        ///  Trying to give a discrete description for the strength of any given predicted attack.
        /// </summary>
        public enum PredictedAttackStrength
        {
            None,
            Low,
            Medium,
            High,
            Overwhelming
        };


    }
}
