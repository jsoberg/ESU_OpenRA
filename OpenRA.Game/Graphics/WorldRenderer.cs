#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
    /** A version of the WorldRenderer which has all graphics removed from it. */
    public class WorldRenderer : IDisposable
    {
        public static readonly Func<IRenderable, int> RenderableScreenZPositionComparisonKey =
            r => ZPosition(r.Pos, r.ZOffset);

        public readonly Size TileSize;
        public readonly World World;
        readonly Lazy<DeveloperMode> devTrait;

        // Null variables.
        public readonly Theater Theater = null;
        public Viewport Viewport = null;

        // Never used.
        public event Action PaletteInvalidated = null;
        readonly HardwarePalette palette = null;
        readonly Dictionary<string, PaletteReference> palettes = null;
        readonly TerrainRenderer terrainRenderer = null;
        readonly Func<string, PaletteReference> createPaletteReference = null;

        internal WorldRenderer(World world)
        {
            World = world;
            TileSize = World.Map.Grid.TileSize;
            devTrait = Exts.Lazy(() => world.LocalPlayer != null ? world.LocalPlayer.PlayerActor.Trait<DeveloperMode>() : null);
        }

        public void UpdatePalettesForPlayer(string internalName, HSLColor color, bool replaceExisting)
        {
            /* Do nothing. */
        }

        PaletteReference CreatePaletteReference(string name)
        {
            /* Do nothing. */
            return null;
        }

        public PaletteReference Palette(string name)
        {
            /* Do nothing. */
            return null;
        }

        public void AddPalette(string name, ImmutablePalette pal, bool allowModifiers = false, bool allowOverwrite = false)
        {
            /* Do nothing. */
        }

        public void ReplacePalette(string name, IPalette pal)
        {
            /* Do nothing. */
        }

        List<IFinalizedRenderable> GenerateRenderables()
        {
            /* Do nothing. */
            return new List<IFinalizedRenderable>();
        }

        public void Draw()
        {
            /* Do nothing. */
        }

        public void RefreshPalette()
        {
            /* Do nothing. */
        }

        // Conversion between world and screen coordinates
        public float2 ScreenPosition(WPos pos)
        {
            return new float2(TileSize.Width * pos.X / 1024f, TileSize.Height * (pos.Y - pos.Z) / 1024f);
        }

        public int2 ScreenPxPosition(WPos pos)
        {
            // Round to nearest pixel
            var px = ScreenPosition(pos);
            return new int2((int)Math.Round(px.X), (int)Math.Round(px.Y));
        }

        // For scaling vectors to pixel sizes in the voxel renderer
        public void ScreenVectorComponents(WVec vec, out float x, out float y, out float z)
        {
            x = TileSize.Width * vec.X / 1024f;
            y = TileSize.Height * (vec.Y - vec.Z) / 1024f;
            z = TileSize.Height * vec.Z / 1024f;
        }

        // For scaling vectors to pixel sizes in the voxel renderer
        public float[] ScreenVector(WVec vec)
        {
            float x, y, z;
            ScreenVectorComponents(vec, out x, out y, out z);
            return new[] { x, y, z, 1f };
        }

        public int2 ScreenPxOffset(WVec vec)
        {
            // Round to nearest pixel
            float x, y, z;
            ScreenVectorComponents(vec, out x, out y, out z);
            return new int2((int)Math.Round(x), (int)Math.Round(y));
        }

        public float ScreenZPosition(WPos pos, int offset)
        {
            return ZPosition(pos, offset) * TileSize.Height / 1024f;
        }

        static int ZPosition(WPos pos, int offset)
        {
            return pos.Y + pos.Z + offset;
        }

        /// <summary>
        /// Returns a position in the world that is projected to the given screen position.
        /// There are many possible world positions, and the returned value chooses the value with no elevation.
        /// </summary>
        public WPos ProjectedPosition(int2 screenPx)
        {
            return new WPos(1024 * screenPx.X / TileSize.Width, 1024 * screenPx.Y / TileSize.Height, 0);
        }

        public void Dispose()
        {
            // HACK: Disposing the world from here violates ownership
            // but the WorldRenderer lifetime matches the disposal
            // behavior we want for the world, and the root object setup
            // is so horrible that doing it properly would be a giant mess.
            World.Dispose();
        }
    }
}
