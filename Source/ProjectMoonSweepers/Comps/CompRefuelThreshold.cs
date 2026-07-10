using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMoonSweepers
{
    public class CompProperties_RefuelThreshold : CompProperties
    {
        public CompProperties_RefuelThreshold()
        {
            this.compClass = typeof(CompRefuelThreshold);
        }
    }

    /// <summary>
    /// Stocke, par Sweeper, le seuil de carburant vital en dessous duquel le
    /// pawn ira se réapprovisionner automatiquement à un robinet. Réglable par
    /// le joueur via un gizmo (slider), à la manière du seuil de réserve
    /// psychique.
    /// </summary>
    public class CompRefuelThreshold : ThingComp
    {
        private float threshold = 0.5f;

        public float Threshold => threshold;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref threshold, "PM_refuelThreshold", 0.5f);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
                yield return g;

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled)
                yield break;

            yield return new Gizmo_RefuelThreshold(this);
        }

        public void SetThreshold(float value)
        {
            threshold = Mathf.Clamp01(value);
        }
    }
}
