using System.Collections.Generic;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Gère la reconstruction des réseaux de tuyaux sur une map.
    /// Quand un tuyau/bâtiment est ajouté ou retiré, on reconstruit
    /// les réseaux par propagation (flood-fill) sur les cellules adjacentes.
    /// </summary>
    public class PipeNetManager : MapComponent
    {
        private List<PipeNet> nets = new List<PipeNet>();
        private bool dirty = true;

        public PipeNetManager(Map map) : base(map) { }

        public void NotifyConnectorSpawned(CompPipeConnector c) { dirty = true; }
        public void NotifyConnectorDespawned(CompPipeConnector c) { dirty = true; }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // Reconstruction throttlée : 1x par 60 ticks si dirty
            if (dirty && Find.TickManager.TicksGame % 60 == 0)
            {
                RebuildNets();
                dirty = false;
            }
        }

        private List<CompPipeConnector> AllConnectors()
        {
            List<CompPipeConnector> result = new List<CompPipeConnector>();
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                ThingWithComps twc = things[i] as ThingWithComps;
                if (twc == null) continue;
                CompPipeConnector c = twc.GetComp<CompPipeConnector>();
                if (c != null) result.Add(c);
            }
            return result;
        }

        private void RebuildNets()
        {
            nets.Clear();
            List<CompPipeConnector> all = AllConnectors();
            HashSet<CompPipeConnector> visited = new HashSet<CompPipeConnector>();

            foreach (CompPipeConnector start in all)
            {
                if (visited.Contains(start)) continue;

                PipeNet net = new PipeNet { map = map };
                Queue<CompPipeConnector> queue = new Queue<CompPipeConnector>();
                queue.Enqueue(start);
                visited.Add(start);

                while (queue.Count > 0)
                {
                    CompPipeConnector cur = queue.Dequeue();
                    net.connectors.Add(cur);
                    cur.pipeNet = net;

                    foreach (CompPipeConnector neighbor in FindAdjacentConnectors(cur, all))
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                nets.Add(net);
            }
        }

        private IEnumerable<CompPipeConnector> FindAdjacentConnectors(
            CompPipeConnector c, List<CompPipeConnector> all)
        {
            Building b = c.parent as Building;
            if (b == null) yield break;

            HashSet<IntVec3> myCells = new HashSet<IntVec3>(b.OccupiedRect().Cells);
            // Cellules adjacentes (le pourtour)
            HashSet<IntVec3> adjacentCells = new HashSet<IntVec3>();
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(b))
                adjacentCells.Add(cell);

            foreach (CompPipeConnector other in all)
            {
                if (other == c) continue;
                Building ob = other.parent as Building;
                if (ob == null) continue;

                // Connectés si une cellule de l'un touche une cellule de l'autre
                foreach (IntVec3 ocell in ob.OccupiedRect().Cells)
                {
                    if (adjacentCells.Contains(ocell) || myCells.Contains(ocell))
                    {
                        yield return other;
                        break;
                    }
                }
            }
        }
    }
}
