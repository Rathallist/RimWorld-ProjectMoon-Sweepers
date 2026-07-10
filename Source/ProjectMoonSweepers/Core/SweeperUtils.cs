using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Méthodes utilitaires partagées entre les patches et composants.
    /// Centralisées ici pour éviter la duplication et faciliter la maintenance.
    /// </summary>
    public static class SweeperUtils
    {
        // ── DefNames des gènes Sweeper ────────────────────────────────────────
        private const string GeneLiquidBody   = "PM_LiquidBody";
        private const string GeneOrganicDrain = "PM_OrganicDrain";
        private const string GeneNightborn    = "PM_Nightborn";
        private const string GeneSuitBound    = "PM_SuitBound";

        // ── Vérification rapide : est-ce un Sweeper ? ─────────────────────────

        /// <summary>
        /// Retourne true si le pawn possède le gène PM_LiquidBody.
        /// C'est le gène fondateur — tous les Sweepers l'ont.
        /// </summary>
        public static bool IsSweeper(Pawn pawn)
        {
            return HasGene(pawn, GeneLiquidBody);
        }

        /// <summary>
        /// Retourne true si le pawn possède le gène spécifié.
        /// Fonctionne uniquement avec Biotech (GeneSet).
        /// </summary>
        public static bool HasGene(Pawn pawn, string geneDefName)
        {
            if (pawn?.genes == null) return false;

            GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
            if (def == null)
            {
                Log.ErrorOnce(
                    $"[ProjectMoon Sweepers] GeneDef introuvable : {geneDefName}",
                    geneDefName.GetHashCode()
                );
                return false;
            }

            return pawn.genes.HasActiveGene(def);
        }

        // ── Accès au Need_RedLiquid ───────────────────────────────────────────

        /// <summary>
        /// Retourne le Need_RedLiquid du pawn, ou null s'il n'en a pas.
        /// </summary>
        public static Need_RedLiquid? GetRedLiquidNeed(Pawn pawn)
        {
            return pawn.needs?.TryGetNeed<Need_RedLiquid>();
        }

        /// <summary>
        /// Ajoute du Carburant Vital au pawn si possible.
        /// Retourne true si le gain a été appliqué.
        /// </summary>
        public static bool GainRedLiquid(Pawn pawn, float amount)
        {
            Need_RedLiquid? need = GetRedLiquidNeed(pawn);
            if (need == null) return false;

            need.GainRedLiquid(amount);
            return true;
        }

        // ── Vérifications de capacité ─────────────────────────────────────────

        /// <summary>
        /// Retourne true si le pawn est un Sweeper en état critique
        /// (Carburant Vital sous le seuil critique). Utilisé pour les alertes UI.
        /// </summary>
        public static bool IsInLiquidCrisis(Pawn pawn)
        {
            if (!IsSweeper(pawn)) return false;
            Need_RedLiquid? need = GetRedLiquidNeed(pawn);
            return need != null && need.RedLiquidLevel <= Need_RedLiquid.ThresholdCritical;
        }

        /// <summary>
        /// Retourne true si le pawn est en carence (entre Low et Critical).
        /// </summary>
        public static bool IsLiquidLow(Pawn pawn)
        {
            if (!IsSweeper(pawn)) return false;
            Need_RedLiquid? need = GetRedLiquidNeed(pawn);
            return need != null
                && need.RedLiquidLevel > Need_RedLiquid.ThresholdCritical
                && need.RedLiquidLevel <= Need_RedLiquid.ThresholdLow;
        }
    }
}
