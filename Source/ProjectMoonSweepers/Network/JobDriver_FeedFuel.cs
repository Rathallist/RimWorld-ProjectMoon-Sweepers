using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Un pawn prend un bidon de carburant vital (TargetA) et l'administre à un
    /// Sweeper downed ou alité (TargetB), rechargeant son Need_RedLiquid.
    /// </summary>
    public class JobDriver_FeedFuel : JobDriver
    {
        private const TargetIndex FuelInd = TargetIndex.A;
        private const TargetIndex PatientInd = TargetIndex.B;
        private const int FeedTicks = 240;

        private Thing Fuel => job.GetTarget(FuelInd).Thing;
        private Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed))
                return false;
            if (!pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed))
                return false;
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(FuelInd);
            this.FailOnDestroyedNullOrForbidden(PatientInd);

            // Aller chercher le bidon de carburant et le prendre
            yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(FuelInd);
            yield return Toils_Haul.StartCarryThing(FuelInd);

            // Aller vers le patient
            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(PatientInd);

            // Administrer le carburant
            Toil feed = Toils_General.Wait(FeedTicks);
            feed.WithProgressBarToilDelay(PatientInd);
            feed.FailOnDespawnedNullOrForbidden(PatientInd);
            yield return feed;

            Toil finalize = new Toil();
            finalize.initAction = () =>
            {
                Need_RedLiquid need = Patient.needs?.TryGetNeed<Need_RedLiquid>();
                if (need == null || Fuel == null) return;

                // Chaque unité de carburant rend 1% de jauge
                int stack = Fuel.stackCount;
                float restored = stack / 100f;
                need.GainRedLiquid(restored);

                // Consomme le bidon
                Fuel.Destroy();

                Messages.Message(
                    $"{Patient.LabelShort} a reçu du carburant vital.",
                    Patient, MessageTypeDefOf.PositiveEvent);
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}
