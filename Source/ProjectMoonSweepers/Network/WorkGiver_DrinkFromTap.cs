using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Donne aux Sweepers en manque de Carburant Vital le job d'aller boire
    /// à un robinet connecté à un réseau qui contient du Carburant Vital.
    /// </summary>
    public class WorkGiver_DrinkFromTap : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!SweeperUtils.HasGene(pawn, "PM_LiquidBody")) return true;
            Need_RedLiquid need = pawn.needs?.TryGetNeed<Need_RedLiquid>();
            if (need == null) return true;

            // Seuil réglable par le joueur (défaut 50% si le comp est absent).
            float threshold = 0.5f;
            CompRefuelThreshold thresholdComp = pawn.GetComp<CompRefuelThreshold>();
            if (thresholdComp != null)
                threshold = thresholdComp.Threshold;

            // Ne va boire que si en dessous du seuil.
            return need.CurLevel > threshold;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            ThingWithComps twc = t as ThingWithComps;
            if (twc == null) return false;
            CompPipeTap tap = twc.GetComp<CompPipeTap>();
            if (tap == null) return false;
            if (tap.AvailableInNet < 1f) return false;
            if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly)) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(PM_JobDefOf.PM_DrinkFromTap, t);
        }
    }

    [DefOf]
    public static class PM_JobDefOf
    {
        public static JobDef PM_DrinkFromTap;
        static PM_JobDefOf() { DefOfHelper.EnsureInitializedInCtor(typeof(PM_JobDefOf)); }
    }
}
