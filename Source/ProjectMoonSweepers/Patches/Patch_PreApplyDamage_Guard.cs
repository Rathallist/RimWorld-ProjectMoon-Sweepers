using HarmonyLib;
using Verse;
using System;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Finalizer autour de Pawn_HealthTracker.PreApplyDamage.
    /// Un Finalizer Harmony s'exécute APRÈS l'original et reçoit l'exception
    /// éventuelle. En la "consommant" (return null), on empêche le crash
    /// de casser le JobDriver, et on logge la vraie cause une fois.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_PreApplyDamage_Guard
    {
        private static bool loggedOnce = false;

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, Pawn_HealthTracker __instance, DamageInfo dinfo)
        {
            if (__exception != null)
            {
                if (!loggedOnce)
                {
                    loggedOnce = true;
                    Pawn victim = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                    string victimInfo = victim?.def?.defName ?? "null";
                    string instigatorInfo = dinfo.Instigator?.def?.defName ?? "null";
                    string weaponInfo = (dinfo.Weapon)?.defName ?? "null";
                    Log.Warning(
                        $"[PM Sweepers] PreApplyDamage exception interceptée.\n" +
                        $"  Victime: {victimInfo}\n" +
                        $"  Instigateur: {instigatorInfo}\n" +
                        $"  Arme: {weaponInfo}\n" +
                        $"  Exception: {__exception}");
                }
                // Consommer l'exception : le jeu continue sans crash
                return null;
            }
            return null;
        }
    }
}
