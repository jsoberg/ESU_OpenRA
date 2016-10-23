using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Strategy;

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

        }

        private void AddCreateGroupOrder(Queue<Order> orders, List<Actor> actorsToGroup)
        {
            var createGroupOrder =  new Order("CreateGroup", selfPlayer.PlayerActor, false)
            {
                TargetString = actorsToGroup.Select(a => a.ActorID).JoinWith(",")
            };
            orders.Enqueue(createGroupOrder);
        }

        private void AddAttackMoveOrders(Queue<Order> orders, List<Actor> attackActors, CPos targetPosition)
        {
            foreach (Actor actor in attackActors) {
                var move = new Order("Move", actor, false) { TargetLocation = targetPosition };
                orders.Enqueue(move);
            }
        }
    }
}
