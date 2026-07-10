using UnityEngine;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Gizmo affichant un slider pour régler le seuil de réapprovisionnement
    /// automatique en carburant vital du Sweeper (0-100%).
    ///
    /// Utilise uniquement les helpers RimWorld (Widgets.*, Text.*) pour éviter
    /// de dépendre directement des modules UnityEngine.IMGUI/TextRendering
    /// (Input, Event, TextAnchor, GUIContent) qui ne sont pas référencés.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_RefuelThreshold : Gizmo
    {
        private readonly CompRefuelThreshold comp;

        public Gizmo_RefuelThreshold(CompRefuelThreshold comp)
        {
            this.comp = comp;
            this.Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 160f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect inner = rect.ContractedBy(6f);

            // Titre
            Text.Font = GameFont.Tiny;
            Rect titleRect = new Rect(inner.x, inner.y, inner.width, 20f);
            Widgets.Label(titleRect, "Seuil de recharge");

            // Valeur affichée au centre
            Rect valRect = new Rect(inner.x + 30f, inner.y + 26f, inner.width - 60f, 24f);
            Text.Font = GameFont.Small;
            Widgets.Label(valRect, $"{Mathf.RoundToInt(comp.Threshold * 100f)}%");

            // Bouton "-" (baisse le seuil de 5%)
            Rect minusRect = new Rect(inner.x, inner.y + 26f, 26f, 24f);
            if (Widgets.ButtonText(minusRect, "-"))
                comp.SetThreshold(comp.Threshold - 0.05f);

            // Bouton "+" (monte le seuil de 5%)
            Rect plusRect = new Rect(inner.xMax - 26f, inner.y + 26f, 26f, 24f);
            if (Widgets.ButtonText(plusRect, "+"))
                comp.SetThreshold(comp.Threshold + 0.05f);

            // Légende
            Text.Font = GameFont.Tiny;
            Rect legendRect = new Rect(inner.x, inner.y + 52f, inner.width, 18f);
            Widgets.Label(legendRect, "recharge sous ce seuil");

            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
