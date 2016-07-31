using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Mods.Common.AI.Esu.Rules;

/// <summary>
///  This class is the implementation of the modular ESU AI, with a ruleset described at the project's <see href="https://github.com/jsoberg/ESU_OpenRA/wiki/AI-Rules">GitHub Wiki</see>.
/// </summary>
namespace OpenRA.Mods.Common.AI.Esu
{
    public sealed class EsuAI : ITick, IBot, INotifyDamage, INotifyDiscovered, INotifyOtherProduction
    {
        private readonly EsuAIInfo info;
        private readonly World world;
        private readonly StrategicWorldState worldState;

        // Rulesets.
        private readonly List<BaseEsuAIRuleset> rulesets;

        private Player selfPlayer;
        private bool isEnabled;
        private int tickCount;

        public EsuAI(EsuAIInfo info, ActorInitializer init)
        {
            this.info = info;
            this.world = init.World;
            this.worldState = new StrategicWorldState();

            rulesets = new List<BaseEsuAIRuleset>();
            addRulesets();
        }

        private void addRulesets()
        {
            rulesets.Add(new EsuAIBuildRuleset(world, info));
            rulesets.Add(new EsuAIUnitRuleset(world, info));
        }

        IBotInfo IBot.Info
        {
            get { return info; }
        }

        void IBot.Activate(Player p)
        {
            isEnabled = true;
            selfPlayer = p;
            worldState.Initalize(world, p);

            foreach (BaseEsuAIRuleset rs in rulesets) {
                rs.Activate(p);
            }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {

        }

        void INotifyDiscovered.OnDiscovered(Actor self, Player discoverer, bool playNotification)
        {

        }

        void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced)
        {
            var notifyOtherProductionRulesets = rulesets.Where(a => a is INotifyOtherProduction);
            foreach (INotifyOtherProduction rs in notifyOtherProductionRulesets) {
                rs.UnitProducedByOther(self, producer, produced);
            }
        }

        void ITick.Tick(Actor self)
        {
            if (!isEnabled) {
                return;
            }

            tickCount++;

            // Check for initial tick.
            if (tickCount == 1) {
                DeployMcv(self);
            }

            // Get and issue orders.
            Queue<Order> orders = new Queue<Order>();
            foreach (BaseEsuAIRuleset rs in rulesets) {
                rs.Tick(self, orders);
            }

            foreach (Order order in orders) {
                world.IssueOrder(order);
            }
        }

        private void DeployMcv(Actor self)
        {
            var mcv = world.Actors.FirstOrDefault(a => a.Owner == self.Owner && a.Info.Name == "mcv");

            if (mcv != null) {
                world.IssueOrder(new Order("DeployTransform", mcv, true));
            } else {
                throw new ArgumentNullException("Cannot find MCV");
            }
        }
    }

    public sealed class EsuAIInfo : IBotInfo, ITraitInfo
    {
        private const string AI_NAME = "ESU AI";

        [Desc("Minimum excess power we should maintain.")]
        public readonly int MinimumExcessPower = 100;

        // TODO: Do we care about this?
        [Desc("Radius in cells around the center of the base to expand.")]
        public readonly int MaxBaseRadius = 20;

        string IBotInfo.Name
        {
            get { return AI_NAME; }
        }

        object ITraitInfo.Create(ActorInitializer init)
        {
            return new EsuAI(this, init);
        }
    }
}
