using RimWorld;
using Verse;
using UnityEngine;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Besoin en Carburant Vital pour les Sweepers.
    /// 
    /// Remplace le besoin Food sur les pawns portant PM_LiquidBody.
    /// - Décroit passivement à un taux fixe (pas de faim biologique).
    /// - Déclenche des Hediffs selon le seuil :
    ///     > 0.30 : normal
    ///     0.10 - 0.30 : PM_RedLiquidLow  (malus stats, ralentissement)
    ///     0.00 - 0.10 : PM_RedLiquidCritical (paralysé, Moving = 0)
    ///     0.00 : mort
    /// - Ne peut PAS être rechargé par de la nourriture ordinaire.
    /// - Rechargé par : coups de Hook sur entités organiques (via Patch melee)
    ///                  ou visite d'un LiquidReservoir (via JobDriver à faire)
    /// </summary>
    public class Need_RedLiquid : Need
    {
        // ── Constantes ────────────────────────────────────────────────────────

        /// <summary>Taux de décroissance par tick (1 jour = 60 000 ticks).</summary>
        /// 0.10 / jour → 0.10 / 60000 ≈ 0.00000167 / tick
        private const float FallPerTick = 0.10f / 60000f;

        /// <summary>Seuil bas : déclenche PM_RedLiquidLow.</summary>
        public const float ThresholdLow = 0.30f;

        /// <summary>Seuil critique : déclenche PM_RedLiquidCritical.</summary>
        public const float ThresholdCritical = 0.10f;

        // ── DefNames des Hediffs ──────────────────────────────────────────────
        private static readonly string HediffLowDefName      = "PM_RedLiquidLow";
        private static readonly string HediffCriticalDefName = "PM_RedLiquidCritical";

        // ── Propriétés publiques ──────────────────────────────────────────────

        public override int GUIChangeArrow => CurLevel < ThresholdLow ? -1 : 0;

        // Force l'affichage dans la liste des besoins
        // N'afficher que sur les pawns qui ont le gène Sweeper
        public override bool ShowOnNeedList =>
            pawn?.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("PM_LiquidBody", false)) == true;

        // Couleur de la barre : rouge carburant vital
        private static readonly UnityEngine.Color BarColor = new UnityEngine.Color(0.85f, 0.12f, 0.10f);

        /// <summary>Niveau actuel entre 0 (vide) et 1 (plein).</summary>
        public float RedLiquidLevel
        {
            get => CurLevel;
            set => CurLevel = Mathf.Clamp01(value);
        }

        // ── Constructeur ──────────────────────────────────────────────────────

        public Need_RedLiquid(Pawn pawn) : base(pawn) { }

        // ── Logique principale ────────────────────────────────────────────────

        public override void NeedInterval()
        {
            // Décroissance passive — s'applique même quand le pawn est inactif
            if (!IsFrozen)
            {
                // NeedInterval est appelé toutes les 150 ticks.
                // Un réservoir dorsal augmente la capacité effective : la jauge
                // descend plus lentement (la même quantité de carburant dure plus).
                float capacityMult = FuelCapacityHelper.GetCapacityMultiplier(pawn);
                CurLevel -= (FallPerTick * 150f) / capacityMult;

                // Hémorragie = perte de carburant vital (les Sweepers n'ont pas
                // de sang). Plus le pawn saigne, plus il perd de carburant.
                float bleedRate = pawn.health?.hediffSet?.BleedRateTotal ?? 0f;
                if (bleedRate > 0f)
                {
                    // BleedRateTotal ~ unités de sang/jour. On le convertit en
                    // perte de carburant sur l'intervalle (150 ticks).
                    float bleedLoss = bleedRate * (150f / 60000f) * 0.5f;
                    CurLevel = Mathf.Clamp01(CurLevel - bleedLoss);
                }

                // Les Sweepers n'ont pas de sang : retirer tout effet de perte
                // de sang vanilla (BloodLoss) — c'est le carburant qui gère tout.
                RemoveBloodLoss();
            }

            // Mise à jour des Hediffs selon le niveau actuel
            UpdateHediffs();

            // Régénération : tant qu'il y a du carburant (pas critique),
            // les blessures se referment doucement. À sec, pas de régén.
            if (CurLevel > ThresholdCritical)
            {
                RegenerateInjuries();
            }

            // Mort si le niveau atteint zéro
            if (CurLevel <= 0f && pawn.IsColonist)
            {
                KillFromLiquidDeprivation();
            }
        }

        // ── Gestion des Hediffs ───────────────────────────────────────────────

        private void UpdateHediffs()
        {
            if (CurLevel <= ThresholdCritical)
            {
                // Critique : ajouter critical, retirer low
                EnsureHediff(HediffCriticalDefName, true);
                EnsureHediff(HediffLowDefName,      false);
            }
            else if (CurLevel <= ThresholdLow)
            {
                // Low : ajouter low, retirer critical
                EnsureHediff(HediffLowDefName,      true);
                EnsureHediff(HediffCriticalDefName, false);
            }
            else
            {
                // Normal : retirer les deux
                EnsureHediff(HediffLowDefName,      false);
                EnsureHediff(HediffCriticalDefName, false);
            }
        }

        /// <summary>
        /// Ajoute ou retire un Hediff sur le pawn.
        /// </summary>
        private void EnsureHediff(string defName, bool shouldHave)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                Log.ErrorOnce(
                    $"[ProjectMoon Sweepers] HediffDef introuvable : {defName}",
                    defName.GetHashCode()
                );
                return;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);

            if (shouldHave && existing == null)
            {
                // Ajouter le hediff sur le torso (cible le corps entier)
                Hediff hediff = HediffMaker.MakeHediff(def, pawn, null);
                pawn.health.AddHediff(hediff, null, null);
            }
            else if (!shouldHave && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        // ── Suppression du BloodLoss vanilla ──────────────────────────────────

        /// <summary>
        /// Retire le hediff de perte de sang : les Sweepers n'ont pas de sang,
        /// leur survie dépend uniquement du carburant vital.
        /// </summary>
        private void RemoveBloodLoss()
        {
            if (pawn.health == null) return;
            HediffDef bloodLoss = HediffDefOf.BloodLoss;
            if (bloodLoss == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(bloodLoss);
            if (existing != null)
                pawn.health.RemoveHediff(existing);
        }

        // ── Régénération des blessures ────────────────────────────────────────

        /// <summary>
        /// Referme doucement les blessures du Sweeper tant qu'il a du carburant.
        /// Consomme un peu de carburant proportionnel à la guérison.
        /// </summary>
        private void RegenerateInjuries()
        {
            if (pawn.health == null || pawn.Dead) return;

            // Vitesse de régén par intervalle (NeedInterval = 150 ticks)
            const float healPerInterval = 1.0f;
            const float fuelCostPerHeal = 0.0015f; // carburant consommé par soin

            Hediff_Injury injury = null;
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury inj && inj.CanHealNaturally() && inj.Severity > 0f)
                {
                    injury = inj;
                    break;
                }
            }

            if (injury != null)
            {
                injury.Heal(healPerInterval);
                // La régénération consomme un peu de carburant
                CurLevel = Mathf.Clamp01(CurLevel - fuelCostPerHeal);
            }
        }

        // ── Mort par manque de Carburant Vital ────────────────────────────────────

        private void KillFromLiquidDeprivation()
        {
            if (pawn.Dead) return;

            pawn.Kill(
                new DamageInfo(DamageDefOf.Deterioration, 99999f),
                null
            );

            // Message au joueur
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(
                    "PM_Need_Dissolved".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.NegativeHealthEvent,
                    historical: true
                );
            }
        }

        // ── Méthode publique : recharge ───────────────────────────────────────

        /// <summary>
        /// Appelée par le Patch melee (CompDrain_OnMeleeHit) et les JobDrivers
        /// de ravitaillement au réservoir.
        /// </summary>
        /// <param name="amount">Quantité à ajouter (entre 0 et 1).</param>
        public void GainRedLiquid(float amount)
        {
            CurLevel = Mathf.Clamp01(CurLevel + amount);

            // Retirer les hediffs si on revient au-dessus des seuils
            UpdateHediffs();
        }

        // ── Serialisation ─────────────────────────────────────────────────────

        public override void ExposeData()
        {
            base.ExposeData();
            // CurLevel est géré par la classe de base Need
        }

        // Capacité de base en unités (avant multiplicateur des réservoirs).
        // CurLevel (0-1) × cette base × multiplicateur = unités réelles.
        public const float BaseCapacityUnits = 100f;

        /// <summary>Capacité maximale en unités, réservoirs dorsaux inclus.</summary>
        public float MaxUnits => BaseCapacityUnits * FuelCapacityHelper.GetCapacityMultiplier(pawn);

        /// <summary>Niveau actuel en unités.</summary>
        public float CurUnits => CurLevel * MaxUnits;

        // ── UI ────────────────────────────────────────────────────────────────

        public override void DrawOnGUI(UnityEngine.Rect rect, int maxThresholdMarkers = 8,
            float customMargin = -1f, bool drawArrows = true, bool doTooltip = true,
            UnityEngine.Rect? rectForTooltip = null, bool drawLabel = true)
        {
            // Utilise le rendu de la classe de base mais avec notre couleur
            Verse.Widgets.FillableBar(rect, CurLevel, SolidColorMaterials.NewSolidColorTexture(BarColor));

            // Affiche les unités absolues au centre de la barre.
            UnityEngine.Rect labelRect = rect;
            Verse.Text.Font = Verse.GameFont.Small;
            Verse.Widgets.Label(
                new UnityEngine.Rect(rect.x + 4f, rect.y, rect.width - 8f, rect.height),
                $"{CurUnits:F0} / {MaxUnits:F0}");

            if (doTooltip)
                TooltipHandler.TipRegion(rect, () => GetTipString(), GetHashCode());
        }

        public override string GetTipString()
        {
            string status = CurLevel > ThresholdLow
                ? "PM_Need_StatusNormal".Translate().ToString()
                : CurLevel > ThresholdCritical
                    ? "PM_Need_StatusLow".Translate().ToString()
                    : "PM_Need_StatusCritical".Translate().ToString();

            return "PM_Need_TipBody".Translate(
                CurUnits.ToString("F0"),
                MaxUnits.ToString("F0"),
                (CurLevel * 100f).ToString("F0"),
                status);
        }
    }
}
