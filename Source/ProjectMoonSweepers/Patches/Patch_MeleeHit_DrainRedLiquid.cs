using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Gain de Carburant Vital quand un Sweeper touche une cible organique avec un Hook.
    /// Patch POSTFIX sur ApplyMeleeDamageToTarget : s'exécute après que les dégâts
    /// sont appliqués, avec accès propre au caster et à la cible.
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_MeleeHit_DrainRedLiquid
    {
        private const float GainStandard = 0.05f;  // +5% par coup
        private const float GainBig      = 0.075f;  // +7.5% sur grosse cible

        private static readonly string GeneOrganicDrain = "PM_OrganicDrain";
        private static readonly string HookStandard     = "PM_Hook_Standard";
        private static readonly string HookHeavy        = "PM_Hook_Heavy";

        [HarmonyPostfix]
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            try
            {
                Pawn attacker = __instance?.CasterPawn;
                if (attacker == null || attacker.Dead || !attacker.Spawned) return;
                if (!SweeperUtils.HasGene(attacker, GeneOrganicDrain)) return;

                string weaponDef = attacker.equipment?.Primary?.def?.defName;
                if (weaponDef != HookStandard && weaponDef != HookHeavy) return;

                Pawn victim = target.Thing as Pawn;
                if (victim == null) return;
                if (victim.RaceProps?.IsMechanoid == true) return;

                float gain = (victim.RaceProps?.baseBodySize >= 1.0f) ? GainBig : GainStandard;

                Need_RedLiquid need = attacker.needs?.TryGetNeed<Need_RedLiquid>();
                need?.GainRedLiquid(gain);
            }
            catch { }
        }
    }
}
