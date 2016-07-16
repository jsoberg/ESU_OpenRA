using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIUtils
    {
        public static VisibilityBounds CalculateCurrentVisibleAreaForPlayer(World world, Player owner)
        {
            // Get all Actors owned by specified owner that have the RevealsShroud trait.
            var ownedActors = world.Actors.Where(a => a.Owner == owner && a.IsInWorld 
                && !a.IsDead && a.TraitOrDefault<RevealsShroud>() != null);

            VisibilityBounds bounds = new VisibilityBounds();
            foreach (Actor actor in ownedActors) {
                WDist range = actor.Trait<RevealsShroud>().Range;
                Rect visibleRect = new Rect(actor.CenterPosition, range.Length);
                bounds.AddRect(visibleRect);
            }

            return bounds;
        }
    }
}
