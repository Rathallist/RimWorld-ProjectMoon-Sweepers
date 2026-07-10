using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    public class CompProperties_PipeStubs : CompProperties
    {
        public CompProperties_PipeStubs()
        {
            this.compClass = typeof(CompPipeStubs);
        }
    }

    /// <summary>
    /// Dessine des "prises" (stubs) depuis un bâtiment du réseau vers les
    /// tuyaux adjacents, pour combler l'espace visuel entre eux.
    /// Utilise PostPrintOnto + sprites texturés (fiable, contrairement aux
    /// matériaux couleur unie).
    /// </summary>
    public class CompPipeStubs : ThingComp
    {
        private Graphic stubN, stubS, stubE, stubW;
        private bool loaded = false;
        private int lastMask = -1;

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            Vector2 sz = new Vector2(1f, 1f);
            stubN = GraphicDatabase.Get<Graphic_Single>("Things/Building/PM_PipeStub/PM_PipeStub_north", ShaderDatabase.Cutout, sz, Color.white);
            stubS = GraphicDatabase.Get<Graphic_Single>("Things/Building/PM_PipeStub/PM_PipeStub_south", ShaderDatabase.Cutout, sz, Color.white);
            stubE = GraphicDatabase.Get<Graphic_Single>("Things/Building/PM_PipeStub/PM_PipeStub_east", ShaderDatabase.Cutout, sz, Color.white);
            stubW = GraphicDatabase.Get<Graphic_Single>("Things/Building/PM_PipeStub/PM_PipeStub_west", ShaderDatabase.Cutout, sz, Color.white);
        }

        private bool HasPipeAt(IntVec3 cell)
        {
            if (parent.Map == null || !cell.InBounds(parent.Map)) return false;
            foreach (Thing t in cell.GetThingList(parent.Map))
            {
                ThingWithComps twc = t as ThingWithComps;
                if (twc != null && twc.GetComp<CompPipeConnector>() != null)
                    return true;
            }
            return false;
        }

        // Cellules de bordure adjacentes au bâtiment, par direction
        private IEnumerable<IntVec3> EdgeCells(IntVec3 dir, out IntVec3 outwardOffset)
        {
            outwardOffset = dir;
            CellRect rect = parent.OccupiedRect();
            List<IntVec3> cells = new List<IntVec3>();
            if (dir == IntVec3.North)
                for (int x = rect.minX; x <= rect.maxX; x++) cells.Add(new IntVec3(x, 0, rect.maxZ));
            else if (dir == IntVec3.South)
                for (int x = rect.minX; x <= rect.maxX; x++) cells.Add(new IntVec3(x, 0, rect.minZ));
            else if (dir == IntVec3.East)
                for (int z = rect.minZ; z <= rect.maxZ; z++) cells.Add(new IntVec3(rect.maxX, 0, z));
            else
                for (int z = rect.minZ; z <= rect.maxZ; z++) cells.Add(new IntVec3(rect.minX, 0, z));
            return cells;
        }

        private void PrintStub(SectionLayer layer, Graphic g, IntVec3 edgeCell, IntVec3 outward)
        {
            if (g == null) return;
            Vector3 center = edgeCell.ToVector3Shifted() + outward.ToVector3() * 0.5f;
            center.y = AltitudeLayer.Building.AltitudeFor() - 0.05f;
            Printer_Plane.PrintPlane(layer, center, Vector2.one, g.MatSingle);
        }

        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);
            if (!parent.Spawned) return;
            EnsureLoaded();

            IntVec3[] dirs = { IntVec3.North, IntVec3.South, IntVec3.East, IntVec3.West };
            Graphic[] graphics = { stubN, stubS, stubE, stubW };

            for (int i = 0; i < 4; i++)
            {
                IntVec3 dir = dirs[i];
                foreach (IntVec3 edge in EdgeCells(dir, out IntVec3 outward))
                {
                    if (HasPipeAt(edge + outward))
                        PrintStub(layer, graphics[i], edge, outward);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(120))
            {
                int mask = ConnMask();
                if (mask != lastMask)
                {
                    lastMask = mask;
                    if (parent.Spawned) parent.DirtyMapMesh(parent.Map);
                }
            }
        }

        private int ConnMask()
        {
            int mask = 0;
            IntVec3[] dirs = { IntVec3.North, IntVec3.South, IntVec3.East, IntVec3.West };
            for (int i = 0; i < 4; i++)
                foreach (IntVec3 edge in EdgeCells(dirs[i], out IntVec3 outward))
                    if (HasPipeAt(edge + outward)) mask |= (1 << i);
            return mask;
        }
    }
}
