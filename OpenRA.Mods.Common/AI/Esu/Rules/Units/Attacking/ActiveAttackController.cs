using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttackController
    {
        private readonly List<ActiveAttack> CurrentAttacks;

        public ActiveAttackController()
        {
            this.CurrentAttacks = new List<ActiveAttack>();
        }

        public List<ActiveAttack> GetActiveAttacks()
        {
            return CurrentAttacks;
        }

        public void AddNewActiveAttack(CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            CurrentAttacks.Add(new ActiveAttack(targetPosition, attackTroops));
        }
    }
}
