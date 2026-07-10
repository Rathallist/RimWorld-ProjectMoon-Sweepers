using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Retire la barbe des Sweepers (gène PM_LiquidBody) à la génération
    /// ET à l'apparition sur la map (pour les pawns existants).
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_NoBeard_Generate
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __result)
        {
            RemoveBeardIfSweeper(__result);
        }

        public static void RemoveBeardIfSweeper(Pawn pawn)
        {
            if (pawn?.style == null || pawn.genes == null) return;
            if (!SweeperUtils.HasGene(pawn, "PM_LiquidBody")) return;

            BeardDef noBeard = DefDatabase<BeardDef>.GetNamedSilentFail("NoBeard");
            if (noBeard != null && pawn.style.beardDef != noBeard)
            {
                pawn.style.beardDef = noBeard;
                if (pawn.Spawned)
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }

    /// <summary>
    /// Filet de sécurité : retire la barbe quand un Sweeper apparaît sur la map.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "SpawnSetup", new[] { typeof(Map), typeof(bool) })]
    public static class Patch_NoBeard_Spawn
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            Patch_NoBeard_Generate.RemoveBeardIfSweeper(__instance);
        }
    }
}
