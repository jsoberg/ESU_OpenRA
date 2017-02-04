using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Defense;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    class UnitRuleset : BaseEsuAIRuleset, IUnitProduced, IOrderDeniedListener
    {
        private ScoutHelper scoutHelper;
        private UnitProductionHelper unitHelper;
        private AttackHelper attackHelper;
        private DefenseHelper defenseHelper;

        public UnitRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.scoutHelper = new ScoutHelper(world, selfPlayer, info);
            this.unitHelper = new UnitProductionHelper(world, selfPlayer, info);
            this.attackHelper = new AttackHelper(world, selfPlayer, info);
            this.defenseHelper = new DefenseHelper(world, selfPlayer, info);
        }

        void IUnitProduced.OnUnitProduced(Actor producer, Actor produced)
        {
            if (producer.Owner != selfPlayer) {
                return;
            }

            scoutHelper.UnitProduced(producer, produced);
            unitHelper.UnitProduced(producer, produced);
        }

        void IOrderDeniedListener.OnOrderDenied(Order order)
        {
            unitHelper.OnOrderDenied(order);
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            scoutHelper.AddScoutOrdersIfApplicable(self, state, orders);
            unitHelper.AddUnitOrdersIfApplicable(state, orders);

            // Allow the attack helper to add orders.
            attackHelper.Tick(self, state, orders);

            // Allow the defense helper to add orders.
            defenseHelper.Tick(self, state, orders);

            // Stop harvesters from idling.
            GiveOrdersToIdleHarvesters(orders);
        }

        // Modified slightly from HackyAI.
        private void GiveOrdersToIdleHarvesters(Queue<Order> orders)
        {
            var harvesters = world.ActorsHavingTrait<Harvester>().Where(a => a.Owner == selfPlayer && !a.IsDead);

            // Find idle harvesters and give them orders:
            foreach (var harvester in harvesters)
            {
                var harv = harvester.TraitOrDefault<Harvester>();
                if (harv == null)
                    continue;

                if (!harvester.IsIdle)
                {
                    var act = harvester.GetCurrentActivity();

                    if (act.NextActivity == null || act.NextActivity.GetType() != typeof(FindResources))
                        continue;
                }

                if (!harv.IsEmpty)
                    continue;

                // Tell the idle harvester to quit slacking:
                orders.Enqueue(new Order("Harvest", harvester, false));
            }
        }
    }
}
