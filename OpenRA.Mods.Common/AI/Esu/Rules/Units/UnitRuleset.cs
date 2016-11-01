﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    class UnitRuleset : BaseEsuAIRuleset, INotifyOtherProduction, IOrderDeniedListener
    {
        private ScoutHelper scoutHelper;
        private UnitProductionHelper unitHelper;
        private AttackHelper attackHelper;

        public UnitRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.scoutHelper = new ScoutHelper(world, selfPlayer, info);
            this.unitHelper = new UnitProductionHelper(world, selfPlayer, info);
            this.attackHelper = new AttackHelper(world, selfPlayer, info);
        }

        void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced)
        {
            if (producer.Owner != selfPlayer) {
                return;
            }

            scoutHelper.UnitProduced(self, produced);
        }

        void IOrderDeniedListener.OnOrderDenied(Order order)
        {
            unitHelper.OnOrderDenied(order);
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            scoutHelper.AddScoutOrdersIfApplicable(self, state, orders);
            // Let scout produce units before considering other units.
            if (!scoutHelper.IsScoutBeingProduced()) {
                unitHelper.AddUnitOrdersIfApplicable(self, state, orders);
            }
            // Always allow the attack helper to add orders.
            attackHelper.AddAttackOrdersIfApplicable(self, state, orders);
        }
    }
}
