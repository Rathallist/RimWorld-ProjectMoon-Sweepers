using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    public class HediffCompProperties_EmergencyCrystallizer : HediffCompProperties
    {
        public float bleedRateTrigger = 0.5f;  // seuil de saignement total déclenchant l'implant

        public HediffCompProperties_EmergencyCrystallizer()
        {
            this.compClass = typeof(HediffComp_EmergencyCrystallizer);
        }
    }

    /// <summary>
    /// Surveille le taux d'hémorragie du pawn. Si le saignement total dépasse
    /// le seuil, l'implant s'active : il soigne les blessures qui saignent sur
    /// sa partie du corps, stoppe l'hémorragie, puis se transforme en version
    /// "brisée" (consommé).
    /// </summary>
    public class HediffComp_EmergencyCrystallizer : HediffComp
    {
        public HediffCompProperties_EmergencyCrystallizer Props =>
            (HediffCompProperties_EmergencyCrystallizer)props;

        private const int CheckInterval = 60; // vérifie 1x/seconde

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead) return;
            if (Pawn.IsHashIntervalTick(CheckInterval) == false) return;

            float totalBleed = Pawn.health.hediffSet.BleedRateTotal;
            if (totalBleed >= Props.bleedRateTrigger)
            {
                Activate();
            }
        }

        private void Activate()
        {
            // Soigne toutes les blessures qui saignent (cautérisation d'urgence)
            foreach (Hediff hediff in Pawn.health.hediffSet.hediffs.ListFullCopy())
            {
                Hediff_Injury injury = hediff as Hediff_Injury;
                if (injury != null && injury.Bleeding)
                {
                    injury.Heal(injury.Severity); // stoppe le saignement en soignant
                }
            }

            BodyPartRecord part = parent.Part;

            // Retire l'implant actif et pose la version brisée sur la même partie
            Pawn.health.RemoveHediff(parent);

            HediffDef spentDef = DefDatabase<HediffDef>.GetNamedSilentFail("PM_CrystallizerImplant_Spent");
            if (spentDef != null)
            {
                Hediff spent = HediffMaker.MakeHediff(spentDef, Pawn, part);
                Pawn.health.AddHediff(spent, part);
            }

            Messages.Message(
                $"Le cristalliseur d'urgence de {Pawn.LabelShort} s'est déclenché et a stoppé l'hémorragie.",
                Pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
