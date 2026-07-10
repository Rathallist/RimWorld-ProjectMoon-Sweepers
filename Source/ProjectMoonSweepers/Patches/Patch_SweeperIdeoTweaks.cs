using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Force la culture PM_Culture_Sweeper sur toute idéologie possédant le meme
    /// Sweeper. Branché en postfix sur Ideo.RecachePrecepts.
    ///
    /// IMPORTANT : ce postfix ne fait QUE réassigner un champ (ideo.culture).
    /// Il n'appelle ni RemovePrecept ni RecachePrecepts, donc il NE déclenche
    /// PAS de récursion (c'était la cause du précédent softlock). L'opération
    /// est idempotente : si la culture est déjà la bonne, on ne touche à rien.
    ///
    /// Enregistré manuellement depuis ModInit (vérification d'existence de la
    /// méthode) pour ne pas casser les autres patches si la signature diffère.
    /// </summary>
    public static class SweeperIdeoTweaks
    {
        private const string SweeperMemeDefName    = "PM_Meme_Sweeper";
        private const string SweeperCultureDefName = "PM_Culture_Sweeper";

        public static bool IsSweeperIdeo(Ideo ideo)
        {
            if (ideo?.memes == null) return false;
            MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(SweeperMemeDefName);
            return meme != null && ideo.memes.Contains(meme);
        }

        public static void ApplyCulture(Ideo ideo)
        {
            CultureDef culture = DefDatabase<CultureDef>.GetNamedSilentFail(SweeperCultureDefName);
            if (culture == null || ideo.culture == culture) return;
            ideo.culture = culture;
        }
    }

    /// <summary>
    /// Postfix sur Ideo.RecachePrecepts : force la culture Sweeper.
    /// Enregistré manuellement depuis ModInit.
    /// </summary>
    public static class Patch_Ideo_RecachePrecepts
    {
        public static void Postfix(Ideo __instance)
        {
            if (!SweeperIdeoTweaks.IsSweeperIdeo(__instance))
                return;

            SweeperIdeoTweaks.ApplyCulture(__instance);
        }
    }
}
