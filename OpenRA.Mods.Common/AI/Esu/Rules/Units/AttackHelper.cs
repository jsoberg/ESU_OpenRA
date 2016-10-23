using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using static OpenRA.Mods.Common.AI.Esu.Strategy.ScoutReportLocationGrid;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public class AttackHelper
    {
        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        public AttackHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;
        }

        public void AddAttackOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            // TODO: Debug code to remove.
            if (state.World.GetCurrentLocalTickCount() > 3000 && !OrderIssued)
            {
                CheckStrategicStateForAttack(state, orders);
            }
        }

        bool OrderIssued = false;

        private void CheckStrategicStateForAttack(StrategicWorldState state, Queue<Order> orders)
        {
            ScoutReportLocationGrid reportGrid = state.ScoutReportGrid;
            AggregateScoutReportData bestCell = reportGrid.GetCurrentBestFitCell();

            if (bestCell != null) {
                var attackActors = state.World.ActorsHavingTrait<Armament>().Where(a => a.Owner == selfPlayer && !a.IsDead);
                AddCreateGroupOrder(orders, attackActors);
                AddAttackMoveOrders(orders, attackActors, bestCell.RelativePosition);
                OrderIssued = true;
            }
        }

        private void AddCreateGroupOrder(Queue<Order> orders, IEnumerable<Actor> actorsToGroup)
        {
            var createGroupOrder =  new Order("CreateGroup", selfPlayer.PlayerActor, false)
            {
                TargetString = actorsToGroup.Select(a => a.ActorID).JoinWith(",")
            };
            orders.Enqueue(createGroupOrder);
        }

        private void AddAttackMoveOrders(Queue<Order> orders, IEnumerable<Actor> attackActors, CPos targetPosition)
        {
            foreach (Actor actor in attackActors) {
                var move = new Order("AttackMove", actor, false) { TargetLocation = targetPosition };
                orders.Enqueue(move);
            }
        }
    }
}
