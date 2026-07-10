using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Point d'entrée du mod. Initialise Harmony et enregistre tous les patches.
    /// RimWorld appelle StaticConstructorOnStartup au démarrage du jeu.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ProjectMoonSweepersMod
    {
        public const string HarmonyId = "Althar.ProjectMoonSweepers";

        static ProjectMoonSweepersMod()
        {
            var harmony = new Harmony(HarmonyId);

            // Patches déclarés via attributs [HarmonyPatch]
            harmony.PatchAll();

            // Patch manuel : pensées de faim → inactives pour les Sweepers.
            // On cherche la classe par nom de string pour éviter un crash
            // si elle a été renommée entre versions de RimWorld.
            PatchFoodThoughts(harmony);

            // Patch manuel : force la culture Sweeper sur l'idéo qui a le meme.
            // Vérifie l'existence de RecachePrecepts avant de patcher.
            TryPatchCulture(harmony);

            // Patch manuel : barrière linguistique sur le commerce (TradeSession).
            TryPatchLanguage(harmony);

            // Patch manuel : les Sweepers peuvent toujours nettoyer.
            TryPatchCleaning(harmony);

            Log.Message("[ProjectMoon Sweepers] Mod initialisé. Patches Harmony appliqués.");
        }

        /// <summary>
        /// Patche le getter de Pawn.CombinedDisabledWorkTags (postfix) pour
        /// retirer Cleaning des tags désactivés des Sweepers. Vérifie l'existence
        /// de la propriété avant de patcher.
        /// </summary>
        private static void TryPatchCleaning(Harmony harmony)
        {
            MethodInfo original = AccessTools.PropertyGetter(typeof(Pawn), "CombinedDisabledWorkTags");
            if (original == null)
            {
                Log.Warning("[ProjectMoon Sweepers] Pawn.CombinedDisabledWorkTags introuvable — garantie de nettoyage ignorée.");
                return;
            }

            MethodInfo postfix = typeof(Patch_Pawn_CombinedDisabledWorkTags)
                .GetMethod(nameof(Patch_Pawn_CombinedDisabledWorkTags.Postfix));

            try
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                Log.Message("[ProjectMoon Sweepers] Patch garantie de nettoyage appliqué.");
            }
            catch (System.Exception e)
            {
                Log.Warning("[ProjectMoon Sweepers] Échec du patch garantie de nettoyage (ignoré) : " + e.Message);
            }
        }

        /// <summary>
        /// Patche Dialog_Trade.PostOpen (postfix) pour fermer la fenêtre de
        /// commerce et afficher un message chiffré si le colon et le marchand
        /// ne partagent pas la même langue (sauf si Traduction recherchée).
        /// </summary>
        private static void TryPatchLanguage(Harmony harmony)
        {
            MethodInfo original = AccessTools.Method(typeof(Dialog_Trade), "PostOpen");
            if (original == null)
            {
                Log.Warning("[ProjectMoon Sweepers] Dialog_Trade.PostOpen introuvable — barrière linguistique du commerce ignorée.");
                return;
            }

            MethodInfo postfix = typeof(Patch_Dialog_Trade_PostOpen)
                .GetMethod(nameof(Patch_Dialog_Trade_PostOpen.Postfix));

            try
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                Log.Message("[ProjectMoon Sweepers] Patch barrière linguistique appliqué.");
            }
            catch (System.Exception e)
            {
                Log.Warning("[ProjectMoon Sweepers] Échec du patch barrière linguistique (ignoré) : " + e.Message);
            }
        }

        /// <summary>
        /// Patche Ideo.RecachePrecepts (postfix) pour forcer la culture Sweeper
        /// sur les idéologies possédant le meme Sweeper. Le postfix ne fait que
        /// réassigner ideo.culture (aucune récursion). Vérifie l'existence de la
        /// méthode avant de patcher pour ne pas casser les autres patches.
        /// </summary>
        private static void TryPatchCulture(Harmony harmony)
        {
            MethodInfo original = typeof(Ideo).GetMethod(
                "RecachePrecepts",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (original == null)
            {
                Log.Warning("[ProjectMoon Sweepers] Ideo.RecachePrecepts introuvable — culture Sweeper non forcée.");
                return;
            }

            MethodInfo postfix = typeof(Patch_Ideo_RecachePrecepts)
                .GetMethod(nameof(Patch_Ideo_RecachePrecepts.Postfix));

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.Message("[ProjectMoon Sweepers] Patch culture Sweeper appliqué.");
        }

        /// <summary>
        /// Cherche ThoughtWorker_FoodWanting (ou son équivalent selon la version)
        /// et patche CurrentStateInternal pour retourner Inactive sur les Sweepers.
        /// Ne fait rien si la classe n'existe pas dans cette version du jeu.
        /// </summary>
        private static void PatchFoodThoughts(Harmony harmony)
        {
            // Noms possibles selon la version de RimWorld
            string[] candidateTypeNames = new[]
            {
                "RimWorld.ThoughtWorker_FoodWanting",
                "RimWorld.ThoughtWorker_Hungry",      // Nom alternatif possible en 1.6
            };

            Type? thoughtWorkerType = null;
            foreach (string typeName in candidateTypeNames)
            {
                thoughtWorkerType = GenTypes.GetTypeInAnyAssembly(typeName, null);
                if (thoughtWorkerType != null) break;
            }

            if (thoughtWorkerType == null)
            {
                // Classe absente dans cette version — le suppressedNeeds du gène
                // se charge de bloquer la faim, ce patch est optionnel.
                Log.Message("[ProjectMoon Sweepers] ThoughtWorker faim introuvable — patch optionnel ignoré.");
                return;
            }

            MethodInfo? original = thoughtWorkerType.GetMethod(
                "CurrentStateInternal",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (original == null)
            {
                Log.Warning("[ProjectMoon Sweepers] CurrentStateInternal introuvable sur ThoughtWorker faim.");
                return;
            }

            MethodInfo postfix = typeof(Patch_FoodThoughtWorker_Manual)
                .GetMethod(nameof(Patch_FoodThoughtWorker_Manual.Postfix))!;

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.Message($"[ProjectMoon Sweepers] Patch pensées faim appliqué sur {thoughtWorkerType.Name}.");
        }
    }

    /// <summary>
    /// Postfix pour ThoughtWorker_FoodWanting.CurrentStateInternal.
    /// Appliqué manuellement depuis ModInit pour éviter la dépendance au typeof.
    /// </summary>
    public static class Patch_FoodThoughtWorker_Manual
    {
        public static void Postfix(Pawn p, ref ThoughtState __result)
        {
            if (SweeperUtils.IsSweeper(p))
                __result = ThoughtState.Inactive;
        }
    }
}
