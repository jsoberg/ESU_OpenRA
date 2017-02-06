﻿using System.Linq;
using System.Collections.Generic;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Defense;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    class UnitRuleset : BaseEsuAIRuleset, IUnitProduced, IOrderDeniedListener
    {
        private readonly MersenneTwister Random = new MersenneTwister();

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
            GiveOrdersToIdleHarvesters(state, orders);
        }

        private readonly Dictionary<Actor, HashSet<CPos>> PreviouslyTargetedPositionsForHarvesters = new Dictionary<Actor, HashSet<CPos>>();

        // Modified slightly from HackyAI.
        private void GiveOrdersToIdleHarvesters(StrategicWorldState state, Queue<Order> orders)
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

                if (!PreviouslyTargetedPositionsForHarvesters.ContainsKey(harvester))
                {
                    PreviouslyTargetedPositionsForHarvesters.Add(harvester, new HashSet<CPos>());
                }
                
                CPos closest = ClosestResource(state, harvester);
                if (closest == CPos.Invalid) {
                    orders.Enqueue(new Order("Harvest", harvester, false));
                } else {
                    orders.Enqueue(new Order("Harvest", harvester, false) { TargetLocation = closest });
                }
            }
        }

        private CPos ClosestResource(StrategicWorldState state, Actor harvester)
        {
            double minDistance = double.MaxValue;
            CPos minPos = CPos.Invalid;
            foreach (KeyValuePair<ResourceTile, HashSet<CPos>> entry in state.ResourceCache)
            {
                foreach (CPos pos in entry.Value)
                {
                    if (PreviouslyTargetedPositionsForHarvesters[harvester].Contains(pos))
                    {
                        continue;
                    }

                    double dist = GeometryUtils.EuclideanDistance(pos, harvester.Location);
                    if (dist < minDistance) {
                        minDistance = dist;
                        minPos = pos;
                    }
                }
            }
            return minPos;
        }
    }
}
