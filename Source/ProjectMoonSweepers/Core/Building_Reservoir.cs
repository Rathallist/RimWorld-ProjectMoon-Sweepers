using UnityEngine;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Réservoir de carburant vital dont le sprite change selon le niveau
    /// de remplissage (StoredLiquid / Capacity), façon override Graphic.
    /// 6 paliers : 0%, 20%, 40%, 60%, 80%, 100%.
    /// </summary>
    public class Building_Reservoir : Building
    {
        private CompPipeStorage storage;
        private Graphic[] levelGraphics;
        private bool loaded = false;
        private int lastLevel = -1;

        private const int LevelCount = 5;
        private const string PathBase = "Things/Building/PM_BloodReservoir_levels/PM_BloodReservoir_";

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            storage = GetComp<CompPipeStorage>();
        }

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            levelGraphics = new Graphic[LevelCount];
            for (int i = 0; i < LevelCount; i++)
            {
                string path = PathBase + i.ToString();
                levelGraphics[i] = GraphicDatabase.Get<Graphic_Single>(
                    path, ShaderDatabase.Cutout,
                    this.def.graphicData.drawSize, Color.white);
            }
        }

        private int CurrentLevel()
        {
            if (storage == null || storage.Capacity <= 0f) return 0;
            float frac = Mathf.Clamp01(storage.StoredLiquid / storage.Capacity);
            int level = Mathf.RoundToInt(frac * (LevelCount - 1));
            return Mathf.Clamp(level, 0, LevelCount - 1);
        }
        public override Graphic Graphic
        {
            get
            {
                EnsureLoaded();
                int level = CurrentLevel();
                if (levelGraphics != null && levelGraphics[level] != null)
                    return levelGraphics[level];
                return base.Graphic;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(60))
            {
                int level = CurrentLevel();
                if (level != lastLevel)
                {
                    lastLevel = level;
                    if (Spawned)
                        DirtyMapMesh(Map);
                }
            }
        }

    
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // Au démantèlement : si la capacité restante du réseau devient
            // inférieure au carburant stocké, rendre le surplus en items.
            if (mode == DestroyMode.Deconstruct && storage != null && Spawned)
            {
                TryEjectOverflow();
            }
            base.Destroy(mode);
        }

        private void TryEjectOverflow()
        {
            // Ce réservoir contient storedLiquid. À son retrait, ce carburant
            // doit se recaser dans les autres réservoirs. Ce qui ne rentre pas
            // déborde et doit être rendu en items.
            float storedElsewhere = storage.PipeNetTotalStored - storage.StoredLiquid;
            float capacityAfter = storage.PipeNetTotalCapacity - storage.Capacity;
            float freeSpaceElsewhere = capacityAfter - storedElsewhere;

            // Surplus = carburant de ce réservoir qui ne tient pas ailleurs
            float overflow = storage.StoredLiquid - freeSpaceElsewhere;
            if (overflow <= 0f) return;

            int amount = Mathf.FloorToInt(overflow);
            if (amount <= 0) return;

            ThingDef liquidDef = DefDatabase<ThingDef>.GetNamedSilentFail("PM_RedLiquidResource");
            if (liquidDef == null) return;

            // La part qui se redistribue (storedLiquid - overflow) va aux autres
            // réservoirs ; on la retire d'abord d'ici puis on l'ajoute au réseau.
            float redistribute = storage.StoredLiquid - overflow;
            if (redistribute > 0f)
            {
                storage.DrawLiquid(redistribute);
                storage.AddToNetExcludingSelf(redistribute);
            }
            // Le surplus (overflow) quitte ce réservoir et devient des items.
            storage.DrawLiquid(overflow);

            // Spawn les items de carburant vital à la position du réservoir
            while (amount > 0)
            {
                int stack = Mathf.Min(amount, liquidDef.stackLimit);
                Thing liquid = ThingMaker.MakeThing(liquidDef);
                liquid.stackCount = stack;
                GenPlace.TryPlaceThing(liquid, Position, Map, ThingPlaceMode.Near);
                amount -= stack;
            }

            Messages.Message(
                "Le carburant vital excédentaire a été récupéré lors du démantèlement.",
                new TargetInfo(Position, Map), MessageTypeDefOf.NeutralEvent);
        }

    }
}
