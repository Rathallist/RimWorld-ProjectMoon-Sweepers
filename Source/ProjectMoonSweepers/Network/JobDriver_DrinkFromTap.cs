using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMoonSweepers
{
    public class JobDriver_DrinkFromTap : JobDriver
    {
        private const int DrinkTicks = 180;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            Toil drink = Toils_General.Wait(DrinkTicks);
            drink.WithProgressBarToilDelay(TargetIndex.A);
            drink.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return drink;

            Toil finalize = new Toil();
            finalize.initAction = () =>
            {
                ThingWithComps tap = (ThingWithComps)job.targetA.Thing;
                CompPipeTap tapComp = tap.GetComp<CompPipeTap>();
                Need_RedLiquid need = pawn.needs?.TryGetNeed<Need_RedLiquid>();
                if (tapComp != null && need != null)
                {
                    // Combien il manque pour remplir la jauge (0..1 → on convertit en "unités")
                    float missing = (1f - need.CurLevel) * 100f;
                    float drawn = tapComp.DrawFromNet(missing);
                    need.GainRedLiquid(drawn / 100f);
                }
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}
