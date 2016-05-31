using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

/** A simple AI used for testing. */
namespace OpenRA.Mods.Common.AI
{
    public sealed class SimpleTestAI : ITick, IBot, INotifyDamage
    {
        private readonly SimpleTestAIInfo info;

        public SimpleTestAI(SimpleTestAIInfo info, ActorInitializer init)
        {
            this.info = info;
        }

        void ITick.Tick(Actor self)
        {
            
        }

        void IBot.Activate(Player p)
        {

        }

        IBotInfo IBot.Info
        {
            get { return info; }
        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {

        }
    }

    public sealed class SimpleTestAIInfo : IBotInfo, ITraitInfo
    {
        private const string AI_NAME = "Simple Test AI";

        string IBotInfo.Name
        {
            get { return AI_NAME; }
        }

        object ITraitInfo.Create(ActorInitializer init)
        {
            return new SimpleTestAI(this, init);
        }
    }
}
