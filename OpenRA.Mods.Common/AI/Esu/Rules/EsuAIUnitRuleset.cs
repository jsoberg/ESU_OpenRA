﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    class EsuAIUnitRuleset : BaseEsuAIRuleset, INotifyOtherProduction
    {
        private EsuAIScoutHelper scoutHelper;
        private EsuAIUnitHelper unitHelper;

        public EsuAIUnitRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.scoutHelper = new EsuAIScoutHelper(world, selfPlayer, info);
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
        }
    }
}
