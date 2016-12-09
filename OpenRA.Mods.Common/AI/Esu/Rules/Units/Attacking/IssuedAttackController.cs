using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking
{
    public class ActiveAttackController
    {
        private readonly List<IssuedAttack> CurrentAttacks;

        public ActiveAttackController()
        {
            this.CurrentAttacks = new List<IssuedAttack>();
        }

        public List<IssuedAttack> GetActiveAttacks()
        {
            return CurrentAttacks;
        }

        public void AddNewActiveAttack(CPos targetPosition, IEnumerable<Actor> attackTroops)
        {
            CurrentAttacks.Add(new IssuedAttack(targetPosition, attackTroops));
        }
    }
}
