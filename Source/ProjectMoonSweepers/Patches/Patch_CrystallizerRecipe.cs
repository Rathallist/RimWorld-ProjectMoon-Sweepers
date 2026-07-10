using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Quand un bill se termine sur le Cristalliseur, on déduit le carburant
    /// vital correspondant du réseau. Si le réseau n'a pas assez de carburant,
    /// le bill échoue (les produits ne sont pas créés).
    ///
    /// Patch en postfix sur GenRecipe.MakeRecipeProducts. On récupère le billGiver
    /// et la recette pour appliquer le coût. Signature 1.6 :
    /// MakeRecipeProducts(RecipeDef, Pawn, List&lt;Thing&gt;, Thing, IBillGiver,
    ///                    Precept_ThingStyle, ThingStyleDef, int?).
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_CrystallizerRecipe
    {
        // Coût en carburant vital par recette (defName → quantité)
        private static readonly Dictionary<string, float> FuelCosts = new Dictionary<string, float>
        {
            { "PM_Make_Hook_Standard", 40f },
            { "PM_Make_Hook_Heavy", 70f },
            { "PM_EmergencyCrystallizer", 30f }
        };

        // Prefix : vérifie/déduit le carburant AVANT la production.
        // On nomme les paramètres exactement comme la méthode vanilla pour que
        // Harmony les injecte correctement (recipeDef, billGiver).
        public static bool Prefix(RecipeDef recipeDef, IBillGiver billGiver)
        {
            if (recipeDef == null) return true;
            if (!FuelCosts.TryGetValue(recipeDef.defName, out float cost))
                return true; // recette non concernée

            Building building = billGiver as Building;
            if (building == null) return true;

            CompCrystallizerFuel fuelComp = building.GetComp<CompCrystallizerFuel>();
            if (fuelComp == null) return true;

            if (!fuelComp.TryConsumeFuel(cost))
            {
                Messages.Message(
                    $"Production annulée : le réseau manque de carburant vital (besoin de {cost}).",
                    building, MessageTypeDefOf.RejectInput, false);
                return false; // empêche la production
            }

            return true; // carburant déduit, production continue
        }
    }
}
