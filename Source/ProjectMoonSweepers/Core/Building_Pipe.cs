using UnityEngine;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Tuyau de carburant vital qui choisit son sprite selon ses connexions
    /// aux voisins (autres tuyaux ou bâtiments du réseau).
    /// Bitmask des directions : N=1, E=2, S=4, W=8 → 16 sprites.
    /// </summary>
    public class Building_Pipe : Building
    {
        private Graphic[] connGraphics;
        private bool loaded = false;
        private int lastMask = -1;

        private const int Variants = 16;
        private const string PathBase = "Things/Building/PM_BloodPipe_conn/PM_BloodPipe_";

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            connGraphics = new Graphic[Variants];
            for (int i = 0; i < Variants; i++)
            {
                string path = PathBase + i.ToString();
                connGraphics[i] = GraphicDatabase.Get<Graphic_Single>(
                    path, ShaderDatabase.Cutout,
                    this.def.graphicData.drawSize, Color.white);
            }
        }

        /// <summary>Y a-t-il un bâtiment connectable (réseau) à cette cellule ?</summary>
        private bool HasConnectorAt(IntVec3 cell)
        {
            if (!cell.InBounds(Map)) return false;
            foreach (Thing t in cell.GetThingList(Map))
            {
                ThingWithComps twc = t as ThingWithComps;
                if (twc != null && twc.GetComp<CompPipeConnector>() != null)
                    return true;
            }
            return false;
        }

        private int ConnectionMask()
        {
            if (!Spawned) return 0;
            int mask = 0;
            if (HasConnectorAt(Position + IntVec3.North)) mask |= 1;
            if (HasConnectorAt(Position + IntVec3.East)) mask |= 2;
            if (HasConnectorAt(Position + IntVec3.South)) mask |= 4;
            if (HasConnectorAt(Position + IntVec3.West)) mask |= 8;
            return mask;
        }

        public override Graphic Graphic
        {
            get
            {
                EnsureLoaded();
                int mask = ConnectionMask();
                if (connGraphics != null && connGraphics[mask] != null)
                    return connGraphics[mask];
                return base.Graphic;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            // Rafraîchit ce tuyau et ses voisins à l'apparition
            RefreshSelfAndNeighbors();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 pos = Position;
            base.DeSpawn(mode);
            // Rafraîchit les voisins après disparition
            if (map != null)
                RefreshNeighborsAt(map, pos);
        }

        private void RefreshSelfAndNeighbors()
        {
            if (!Spawned) return;
            lastMask = ConnectionMask();
            DirtyMapMesh(Map);
            RefreshNeighborsAt(Map, Position);
        }

        private static void RefreshNeighborsAt(Map map, IntVec3 pos)
        {
            IntVec3[] dirs = { IntVec3.North, IntVec3.East, IntVec3.South, IntVec3.West };
            foreach (IntVec3 d in dirs)
            {
                IntVec3 cell = pos + d;
                if (!cell.InBounds(map)) continue;
                foreach (Thing t in cell.GetThingList(map))
                {
                    if (t is Building_Pipe pipe && pipe.Spawned)
                        pipe.DirtyMapMesh(map);
                }
            }
        }

        protected override void Tick()
        {
            base.Tick();
            // Vérifie périodiquement si les connexions ont changé
            if (this.IsHashIntervalTick(120))
            {
                int mask = ConnectionMask();
                if (mask != lastMask)
                {
                    lastMask = mask;
                    if (Spawned) DirtyMapMesh(Map);
                }
            }
        }
    }
}
