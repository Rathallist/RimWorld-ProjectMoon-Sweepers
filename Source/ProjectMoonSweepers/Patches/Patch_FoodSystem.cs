using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMoonSweepers
{
    // ── Patch 1 : Empêcher la mort par famine sur les Sweepers ───────────────

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.ShouldBeFedBySomeone))]
    public static class Patch_FoodUtility_ShouldBeFed
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (SweeperUtils.IsSweeper(pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }


    // ── Patch 2 : Empêcher les Sweepers de chercher de la nourriture ─────────
    // TryGiveJob retourne un Job (pas ThinkResult) — prefix qui retourne null

    [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
    public static class Patch_JobGiver_GetFood_TryGiveJob
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (SweeperUtils.IsSweeper(pawn))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }


    // ── Patch 3 : ShouldHaveNeed — Food=false, PM_RedLiquid=true ─────────────

    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Patch_NeedsTracker_ShouldHaveNeed
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
        {
            // Pawn_NeedsTracker.pawn est privé en 1.6 → accès via Traverse
            Pawn? pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !SweeperUtils.IsSweeper(pawn)) return;

            if (nd == NeedDefOf.Food || nd == NeedDefOf.Rest)
            {
                __result = false;
                return;
            }

            NeedDef? redLiquidDef = DefDatabase<NeedDef>.GetNamedSilentFail("PM_RedLiquid");
            if (redLiquidDef != null && nd == redLiquidDef)
                __result = true;
        }
    }


    // ── Patch 4 : Forcer la reconstruction des besoins au spawn ──────────────
    // Retire le besoin Food résiduel des Sweepers (pawns existants inclus).

    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    public static class Patch_Pawn_SpawnSetup_Needs
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance?.needs == null) return;
            if (!SweeperUtils.IsSweeper(__instance)) return;

            // Reconstruit la liste des besoins selon ShouldHaveNeed
            // (notre patch désactive Food → il sera retiré).
            __instance.needs.AddOrRemoveNeedsAsAppropriate();
        }
    }
}