using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Pensée situationnelle : malus de mood quand un croyant du précepte
    /// "rester à l'intérieur le jour" se trouve dehors en pleine journée.
    ///
    /// Les Sweepers sont des créatures nocturnes : la Cité les envoie nettoyer
    /// les Backstreets durant la Nuit. Être exposé à la lumière du jour est
    /// une transgression.
    ///
    /// Référencé par le ThoughtDef PM_OutdoorsDaytime_Mood, branché au précepte
    /// via PreceptComp_SituationalThought (donc le précepte est déjà vérifié).
    /// </summary>
    public class ThoughtWorker_OutdoorsDaytime : ThoughtWorker
    {
        // Plage horaire considérée comme "plein jour" (heures RimWorld 0-23).
        private const int DayStartHour = 7;   // 07:00
        private const int DayEndHour   = 18;  // 18:00

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p == null || !p.Spawned || p.Map == null)
                return ThoughtState.Inactive;

            if (!p.Awake())
                return ThoughtState.Inactive;

            // Dans une pièce intérieure → pas de transgression.
            Room room = p.GetRoom();
            if (room != null && !room.PsychologicallyOutdoors)
                return ThoughtState.Inactive;

            // Sous un toit, même hors pièce fermée, on considère protégé.
            if (p.Position.Roofed(p.Map))
                return ThoughtState.Inactive;

            // Vérifier l'heure locale.
            int hour = GenLocalDate.HourOfDay(p);
            if (hour < DayStartHour || hour >= DayEndHour)
                return ThoughtState.Inactive;

            return ThoughtState.ActiveAtStage(0);
        }
    }
}
