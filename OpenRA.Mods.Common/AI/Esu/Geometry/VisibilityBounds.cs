using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Geometry
{
    public class VisibilityBounds
    {
        public static VisibilityBounds CurrentVisibleAreaForPlayer(World world, Player owner)
        {
            // Get all Actors owned by specified owner that have the RevealsShroud trait.
            var ownedActors = world.ActorsHavingTrait<RevealsShroud>().Where(a => a.Owner == owner && a.IsInWorld
                && !a.IsDead);

            VisibilityBounds bounds = new VisibilityBounds();
            foreach (Actor actor in ownedActors) {
                WDist range = actor.Trait<RevealsShroud>().Range;
                Rect visibleRect = new Rect(actor.CenterPosition, range.Length);
                bounds.AddRect(visibleRect);
            }

            return bounds;
        }

        private readonly List<Rect> boundingRects;

        private VisibilityBounds()
        {
            this.boundingRects = new List<Rect>();
        }

        private void AddRect(Rect rect)
        {
            boundingRects.Add(rect);
        }

        public bool ContainsPosition(WPos position)
        {
            foreach (Rect rect in boundingRects) {
                if (rect.ContainsPosition(position)) {
                    return true;
                }
            }

            return false;
        }
    }
}
