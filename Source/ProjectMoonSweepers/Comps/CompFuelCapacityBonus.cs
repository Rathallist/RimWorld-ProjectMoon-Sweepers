using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// ModExtension qui définit le bonus de capacité de carburant vital
    /// apporté par un réservoir dorsal.
    /// </summary>
    public class FuelCapacityExtension : DefModExtension
    {
        public float bonusFraction = 0.3f;
    }

    public class CompProperties_FuelCapacityBonus : CompProperties
    {
        public CompProperties_FuelCapacityBonus()
        {
            this.compClass = typeof(CompFuelCapacityBonus);
        }
    }

    /// <summary>
    /// Comp porté par l'apparel "réservoir dorsal". Sa seule fonction est
    /// d'exister sur l'apparel ; la lecture du bonus se fait via le Need.
    /// </summary>
    public class CompFuelCapacityBonus : ThingComp
    {
    }

    /// <summary>
    /// Helper statique : calcule le multiplicateur de capacité de carburant
    /// vital d'un pawn en sommant les bonus de tous ses réservoirs dorsaux.
    /// </summary>
    public static class FuelCapacityHelper
    {
        public static float GetCapacityMultiplier(Pawn pawn)
        {
            float multiplier = 1f;
            if (pawn?.apparel == null) return multiplier;

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                FuelCapacityExtension ext = apparel.def.GetModExtension<FuelCapacityExtension>();
                if (ext != null)
                    multiplier += ext.bonusFraction;
            }
            return multiplier;
        }
    }
}
