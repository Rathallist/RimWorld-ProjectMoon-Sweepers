using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Garantit qu'un Sweeper n'est jamais incapable de nettoyer : quelle que
    /// soit sa backstory ou ses traits, on retire le WorkTag Cleaning de la
    /// liste des tags de travail désactivés.
    ///
    /// Postfix sur Pawn.CombinedDisabledWorkTags. Enregistré manuellement depuis
    /// ModInit avec vérification d'existence de la propriété.
    /// </summary>
    public static class Patch_Pawn_CombinedDisabledWorkTags
    {
        public static void Postfix(Pawn __instance, ref WorkTags __result)
        {
            if (__instance == null) return;
            if (!SweeperUtils.IsSweeper(__instance)) return;

            // Le WorkType "Cleaning" requiert les tags Cleaning ET ManualDumb.
            // Si l'un des deux est désactivé, le pawn ne peut pas nettoyer.
            // On retire donc les deux pour garantir la capacité de nettoyage.
            __result &= ~WorkTags.Cleaning;
            __result &= ~WorkTags.ManualDumb;
        }
    }
}
