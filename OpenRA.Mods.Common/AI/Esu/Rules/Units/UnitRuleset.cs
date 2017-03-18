using System.Linq;
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
        private const int NumTicksToDisposeResourceUsageLog = 20000;

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

        void IUnitProduced.OnUnitProduced(StrategicWorldState state, Actor producer, Actor produced)
        {
            if (producer.Owner != selfPlayer)
            {
                return;
            }

            scoutHelper.UnitProduced(state, producer, produced);
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

        private readonly List<ResourcePositionUsageLog> PreviouslyTargetedPositionsForHarvesters = new List<ResourcePositionUsageLog>();

        // Modified slightly from HackyAI.
        private void GiveOrdersToIdleHarvesters(StrategicWorldState state, Queue<Order> orders)
        {
            // Allow previously used positions to be reused after a certain period, since resources will respawn.
            PreviouslyTargetedPositionsForHarvesters.RemoveAll(rl => (rl.TickLogged + NumTicksToDisposeResourceUsageLog) <= state.World.GetCurrentLocalTickCount());
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

                CPos closest = ClosestResource(state, harvester);
                if (closest != CPos.Invalid)
                {
                    PreviouslyTargetedPositionsForHarvesters.Add(new ResourcePositionUsageLog(closest, state.World.GetCurrentLocalTickCount()));
                    orders.Enqueue(new Order("Harvest", harvester, false) { TargetLocation = closest });
                }
                else
                {
                    orders.Enqueue(new Order("Harvest", harvester, false));
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
                    if (PreviouslyTargetedPositionsForHarvesters.Any(rl => rl.Position == pos))
                    {
                        continue;
                    }

                    double dist = GeometryUtils.EuclideanDistance(pos, harvester.Location);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        minPos = pos;
                    }
                }
            }
            return minPos;
        }
    }

    class ResourcePositionUsageLog
    {
        public readonly CPos Position;
        public readonly int TickLogged;

        public ResourcePositionUsageLog(CPos position, int tickLogged)
        {
            this.Position = position;
            this.TickLogged = tickLogged;
        }
    }
}
