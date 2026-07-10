using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Un colon va au robinet (TargetA), puise du carburant vital du réseau,
    /// puis va l'administrer à un Sweeper downed/alité (TargetB).
    /// </summary>
    public class JobDriver_FeedFuelFromTap : JobDriver
    {
        private const TargetIndex TapInd = TargetIndex.A;
        private const TargetIndex PatientInd = TargetIndex.B;
        private const int DrawTicks = 120;
        private const int FeedTicks = 180;

        private Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TapInd);
            this.FailOnDespawnedNullOrForbidden(PatientInd);

            // Aller au robinet
            yield return Toils_Goto.GotoThing(TapInd, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TapInd);

            // Puiser du carburant (animation d'attente)
            Toil draw = Toils_General.Wait(DrawTicks);
            draw.WithProgressBarToilDelay(TapInd);
            yield return draw;

            // Aller vers le patient
            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(PatientInd);

            // Administrer
            Toil feed = Toils_General.Wait(FeedTicks);
            feed.WithProgressBarToilDelay(PatientInd);
            feed.FailOnDespawnedNullOrForbidden(PatientInd);
            yield return feed;

            Toil finalize = new Toil();
            finalize.initAction = () =>
            {
                ThingWithComps tap = (ThingWithComps)job.GetTarget(TapInd).Thing;
                CompPipeTap tapComp = tap?.GetComp<CompPipeTap>();
                Need_RedLiquid need = Patient.needs?.TryGetNeed<Need_RedLiquid>();
                if (tapComp == null || need == null) return;

                // Combien il manque, en "unités" (jauge 0..1 → ×100)
                float missing = (1f - need.CurLevel) * 100f;
                float drawn = tapComp.DrawFromNet(missing);
                need.GainRedLiquid(drawn / 100f);

                Messages.Message(
                    $"{Patient.LabelShort} a reçu du carburant vital.",
                    Patient, MessageTypeDefOf.PositiveEvent);
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}
