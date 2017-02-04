using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;
using OpenRA.Mods.Common.AI.Esu.Strategy.Defense;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Defense
{
    public class DefenseHelper : INotifyDamage
    {
        private const long TicksBeforeTakingFurtherAction = 200;

        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private readonly Stack<DefenseAction> DefenseActionStack = new Stack<DefenseAction>();
        private CPos NextDefenseActionLocation = CPos.Invalid;
        private long LastActionTakenTick;

        public DefenseHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;

            // Add to callback list to get damage callbacks.
            DamageNotifier.AddDamageNotificationListener(this);
        }

        public void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (NextDefenseActionLocation != CPos.Invalid 
                && (state.World.GetCurrentLocalTickCount() - LastActionTakenTick) > TicksBeforeTakingFurtherAction)
            {
                IssueDefenseActionAtLocation(NextDefenseActionLocation, state, orders);
            }
        }

        private void IssueDefenseActionAtLocation(CPos location, StrategicWorldState state, Queue<Order> orders)
        {
            var metric = new BaseLethalityMetric(state, SelfPlayer);
            var defensiveCoverage = metric.CurrentDefenseCoverage_Simple(state, Info.GetDefenseLethalityCoveragePercentage(), state.ActiveAttackController.GetActiveAttacks());
            AddAttackMoveOrders(defensiveCoverage.ActorsNecessaryForDefense, orders, location);

            var da = new DefenseAction(location, state.World.GetCurrentLocalTickCount());
            DefenseActionStack.Push(da);

            NextDefenseActionLocation = CPos.Invalid;
            LastActionTakenTick = state.World.GetCurrentLocalTickCount();
        }

        public void AddAttackMoveOrders(List<Actor> actors, Queue<Order> orders, CPos position)
        {
            foreach (Actor actor in actors)
            {
                var move = new Order("AttackMove", actor, false) { TargetLocation = position };
                orders.Enqueue(move);
            }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            // If one of our buildings is getting damaged, we issue orders to defend it next tick.
            if (IsActorSelfOwnedBuilding(self))
            {
                NextDefenseActionLocation = e.Attacker.Location;
            }
        }

        private bool IsActorSelfOwnedBuilding(Actor actor)
        {
            return actor.Owner == SelfPlayer && 
                (EsuAIUtils.IsActorOfType(World, actor, EsuAIConstants.ProductionCategories.BUILDING)
                || EsuAIUtils.IsActorOfType(World, actor, EsuAIConstants.ProductionCategories.DEFENSE));
        }

        private class DefenseAction
        {
            private readonly CPos DefenseActionLocation;
            private readonly long TickActionTaken;

            public DefenseAction(CPos defenseActionLocation, long tickActionTaken)
            {
                this.DefenseActionLocation = defenseActionLocation;
                this.TickActionTaken = tickActionTaken;
            }
        }
    }
}
