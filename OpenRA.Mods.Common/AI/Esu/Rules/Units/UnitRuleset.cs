using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    class UnitRuleset : BaseEsuAIRuleset, INotifyOtherProduction
    {
        private ScoutHelper scoutHelper;
        private UnitHelper unitHelper;

        public UnitRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.scoutHelper = new ScoutHelper(world, selfPlayer, info);
            this.unitHelper = new UnitHelper(world, selfPlayer, info);
        }

        void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced)
        {
            if (producer.Owner != selfPlayer) {
                return;
            }

            bool wasClaimed = scoutHelper.UnitProduced(self, produced);
            // If scout helper doesn't claim unit, then the unit helper might.
            if (!wasClaimed) {
                unitHelper.UnitProduced(self, produced);
            }
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            scoutHelper.AddScoutOrdersIfApplicable(self, state, orders);
            // Let scout produce units before considering other units.
            if (!scoutHelper.IsScoutBeingProduced()) {
                unitHelper.AddUnitOrdersIfApplicable(self, state, orders);
            }
        }
    }
}
