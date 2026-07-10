using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace ProjectMoonSweepers
{
    public class CompProperties_Liquidifier : CompProperties_PipeConnector
    {
        public float reservoirCapacity = 200f;   // buffer interne avant injection réseau
        public int ticksPerCycle = 600;          // 1 cycle / 10s de jeu
        public int matterPerHopperPerCycle = 2;  // matière consommée par hopper par cycle

        public CompProperties_Liquidifier()
        {
            this.compClass = typeof(CompLiquidifier);
        }
    }

    /// <summary>
    /// Produit du Carburant Vital à partir de la matière organique des hoppers
    /// adjacents, puis l'injecte dans le réseau de tuyaux.
    /// Plus il y a de hoppers approvisionnés, plus la production est rapide.
    /// </summary>
    public class CompLiquidifier : CompPipeConnector
    {
        private float buffer = 0f;
        private int cycleTicks = 0;

        public CompProperties_Liquidifier Props => (CompProperties_Liquidifier)props;

        public override void CompTick()
        {
            base.CompTick();
            cycleTicks++;
            if (cycleTicks < Props.ticksPerCycle) return;
            cycleTicks = 0;
            ProcessAllHoppers();
            InjectIntoNet();
        }

        // Traite TOUS les hoppers adjacents (plus de hoppers = plus de production)
        private void ProcessAllHoppers()
        {
            Building building = parent as Building;
            if (building == null || !building.Spawned) return;
            Map map = building.Map;

            HashSet<Thing> processedHoppers = new HashSet<Thing>();

            foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(building))
            {
                if (!adjCell.InBounds(map)) continue;

                Thing hopper = null;
                Thing matter = null;
                List<Thing> things = adjCell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.def.defName == "PM_LiquidifierHopper" || t.def.defName == "Hopper")
                        hopper = t;
                    else if (IsOrganicMatter(t))
                        matter = t;
                }

                // Chaque hopper unique contribue une fois par cycle
                if (hopper != null && matter != null && !processedHoppers.Contains(hopper))
                {
                    processedHoppers.Add(hopper);
                    int toConsume = Mathf.Min(matter.stackCount, Props.matterPerHopperPerCycle);
                    if (toConsume <= 0) toConsume = 1;
                    float yield = GetYield(matter);
                    matter.SplitOff(toConsume).Destroy();
                    buffer += yield * toConsume;
                }
            }
        }

        private void InjectIntoNet()
        {
            if (buffer <= 0f) return;
            if (pipeNet != null)
            {
                float surplus = pipeNet.AddLiquid(buffer);
                buffer = surplus; // garde ce qui n'a pas pu être stocké
            }
        }

        private bool IsOrganicMatter(Thing t)
        {
            if (t == null || t.def == null) return false;
            if (t is Corpse) return true;
            if (t.def.IsMeat) return true;
            if (t.def.thingCategories != null &&
                t.def.thingCategories.Any(c => c.defName == "BodyParts" || c.defName == "BodyPartsOrgan"))
                return true;
            return false;
        }

        private float GetYield(Thing t)
        {
            if (t is Corpse) return 8f;
            if (t.def.IsMeat) return 1.5f;
            return 5f;
        }

        public override string CompInspectStringExtra()
        {
            string netInfo = pipeNet != null
                ? $"Réseau : {Mathf.FloorToInt(pipeNet.TotalStored)} / {Mathf.FloorToInt(pipeNet.TotalCapacity)} carburant vital"
                : "Non connecté à un réseau";
            string bufferInfo = buffer > 0.5f ? $"\nEn attente d'injection : {Mathf.FloorToInt(buffer)}" : "";
            return netInfo + bufferInfo;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref buffer, "buffer", 0f);
            Scribe_Values.Look(ref cycleTicks, "cycleTicks", 0);
        }
    }
}
