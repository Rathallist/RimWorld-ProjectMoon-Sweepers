using UnityEngine;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Building animé pour la Mother. L'animation par frames ne tourne QUE
    /// pendant la gestation (CompMother.IsProducing == true). Au repos, affiche
    /// le sprite statique. Technique override Graphic (façon wind turbine).
    /// </summary>
    public class Building_Mother : Building
    {
        private CompMother motherComp;
        private int currentFrame = 0;
        private int tickCounter = 0;
        private Graphic[] frameGraphics;
        private Graphic staticGraphic;
        private bool loaded = false;

        // Paramètres d'animation
        private const int FrameCount = 15;
        private const int TicksPerFrame = 20;
        private const string PathBase = "Things/Building/PM_Mother_anim/PM_Mother_";
        private const string StaticPath = "Things/Building/PM_Mother";

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            motherComp = GetComp<CompMother>();
        }

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            staticGraphic = GraphicDatabase.Get<Graphic_Single>(
                StaticPath, ShaderDatabase.Cutout,
                this.def.graphicData.drawSize, Color.white);

            frameGraphics = new Graphic[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                string path = PathBase + i.ToString("00");
                frameGraphics[i] = GraphicDatabase.Get<Graphic_Single>(
                    path, ShaderDatabase.Cutout,
                    this.def.graphicData.drawSize, Color.white);
            }
        }

        private bool IsAnimating => motherComp != null && motherComp.IsProducing;

        public override Graphic Graphic
        {
            get
            {
                EnsureLoaded();
                if (IsAnimating
                    && frameGraphics != null
                    && frameGraphics[currentFrame % FrameCount] != null)
                    return frameGraphics[currentFrame % FrameCount];

                if (staticGraphic != null)
                    return staticGraphic;
                return base.Graphic;
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (IsAnimating)
            {
                tickCounter++;
                if (tickCounter >= TicksPerFrame)
                {
                    tickCounter = 0;
                    currentFrame = (currentFrame + 1) % FrameCount;
                    if (Spawned)
                        DirtyMapMesh(Map);
                }
            }
            else if (currentFrame != 0)
            {
                // Production terminée/annulée : revient au sprite statique
                currentFrame = 0;
                tickCounter = 0;
                if (Spawned)
                    DirtyMapMesh(Map);
            }
        }
    }
}
