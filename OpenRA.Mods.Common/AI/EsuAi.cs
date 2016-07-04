using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

/// <summary>
///  This class is the implementation of the modular ESU AI, with a ruleset described at the project's <see href="https://github.com/jsoberg/ESU_OpenRA/wiki/AI-Rules">GitHub Wiki</see>.
/// </summary>
namespace OpenRA.Mods.Common.AI
{
    public sealed class EsuAI : ITick, IBot, INotifyDamage
    {
        private readonly EsuAiInfo info;
        private readonly World world;

        private bool isEnabled;
        private int tickCount;

        public EsuAI(EsuAiInfo info, ActorInitializer init)
        {
            this.info = info;
            this.world = init.World;
        }


        IBotInfo IBot.Info
        {
            get { return info; }
        }

        void IBot.Activate(Player p)
        {
            isEnabled = true;
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {

        }

        void ITick.Tick(Actor self)
        {
            if (!isEnabled)
                return;

            tickCount++;

            if (tickCount == 1)
            {
                DeployMcv(self);
            }

        }

        void DeployMcv(Actor self)
        {
            var mcv = world.Actors.FirstOrDefault(a => a.Owner == self.Owner && a.Info.Name == "mcv");

            if (mcv != null)
            {
                world.IssueOrder(new Order("DeployTransform", mcv, true));
            }
            else
            {
                throw new ArgumentNullException("Cannot find MCV");
            }
        }

    }

    public sealed class EsuAiInfo : IBotInfo, ITraitInfo
    {
        private const string AI_NAME = "ESU AI";

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
