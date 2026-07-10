using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Déclenche les events idéologiques quand un croyant nettoie de la saleté
    /// ou démantèle un bâtiment, activant les bonus de mood du précepte
    /// "saleté abhorrente" (PreceptComp_SelfTookMemoryThought).
    ///
    /// JobDriver.pawn étant protected, on y accède via Traverse (Harmony).
    /// </summary>
    public static class SweeperIdeoEventUtil
    {
        public static Pawn GetActor(JobDriver driver)
        {
            if (driver == null) return null;
            return Traverse.Create(driver).Field("pawn").GetValue<Pawn>();
        }

        public static void RecordEvent(Pawn pawn, string eventDefName)
        {
            if (pawn == null || pawn.Ideo == null) return;

            HistoryEventDef def = DefDatabase<HistoryEventDef>.GetNamedSilentFail(eventDefName);
            if (def == null) return;

            Find.HistoryEventsManager.RecordEvent(
                new HistoryEvent(def, pawn.Named(HistoryEventArgsNames.Doer)));
        }
    }

    /// <summary>
    /// Nettoyage de saleté → event PM_CleanedFilth. Postfix sur MakeNewToils,
    /// finish action attachée au premier toil. Un éventuel job interrompu donne
    /// un faux positif sans gravité (bonus mineur et temporaire).
    /// </summary>
    [HarmonyPatch(typeof(JobDriver_CleanFilth), "MakeNewToils")]
    public static class Patch_JobDriver_CleanFilth_MakeNewToils
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> toils, JobDriver_CleanFilth __instance)
        {
            Pawn actor = SweeperIdeoEventUtil.GetActor(__instance);
            bool first = true;
            foreach (Toil toil in toils)
            {
                if (first)
                {
                    first = false;
                    toil.AddFinishAction(delegate
                    {
                        SweeperIdeoEventUtil.RecordEvent(actor, "PM_CleanedFilth");
                    });
                }
                yield return toil;
            }
        }
    }

    /// <summary>
    /// Démantèlement → event PM_Deconstructed. Postfix sur FinishedRemoving,
    /// appelée quand le démantèlement réussit.
    /// </summary>
    [HarmonyPatch(typeof(JobDriver_Deconstruct), "FinishedRemoving")]
    public static class Patch_JobDriver_Deconstruct_FinishedRemoving
    {
        public static void Postfix(JobDriver_Deconstruct __instance)
        {
            Pawn actor = SweeperIdeoEventUtil.GetActor(__instance);
            SweeperIdeoEventUtil.RecordEvent(actor, "PM_Deconstructed");
        }
    }
}
