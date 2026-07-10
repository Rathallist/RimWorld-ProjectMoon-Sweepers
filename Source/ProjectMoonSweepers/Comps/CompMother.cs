using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace ProjectMoonSweepers
{
    public class CompProperties_Mother : CompProperties_PipeConnector
    {
        public float fuelToProduce = 150f;      // carburant vital total pour 1 Sweeper
        public int ticksToProduce = 120000;     // ~2 jours de jeu
        public float fuelDrawPerTick = 0.00125f; // carburant tiré du réseau par tick

        public CompProperties_Mother()
        {
            this.compClass = typeof(CompMother);
        }
    }

    /// <summary>
    /// La Mother : machine autonome. Quand activée, elle puise le Carburant Vital
    /// du réseau et produit un Sweeper toute seule, sans intervention de pawn.
    /// La production avance tant qu'il y a du carburant dans le réseau.
    /// </summary>
    public class CompMother : CompPipeConnector
    {
        private bool producing = false;
        private float progress = 0f;       // 0..1
        private float fuelAccumulated = 0f;

        public CompProperties_Mother Props => (CompProperties_Mother)props;
        public bool IsProducing => producing;
        public float Progress => progress;

        // === MODULES (facilities adjacentes) ===

        /// <summary>Compte les modules d'un type donné liés à la Mother (max 3).</summary>
        private int CountModules(string moduleDefName)
        {
            CompAffectedByFacilities affected = parent.GetComp<CompAffectedByFacilities>();
            if (affected == null) return 0;

            int count = 0;
            foreach (Thing facility in affected.LinkedFacilitiesListForReading)
            {
                if (facility?.def?.defName == moduleDefName)
                    count++;
            }
            return Mathf.Min(count, 3);
        }

        private int GestationModules   => CountModules("PM_Module_Gestation");
        private int MultiplicationModules => CountModules("PM_Module_Multiplication");
        private int EnhancementModules => CountModules("PM_Module_Enhancement");

        /// <summary>
        /// Carburant requis par cycle, réduit par les modules de gestation
        /// (chaque module -20%, soit jusqu'à -60% avec 3 modules).
        /// </summary>
        private float EffectiveFuelToProduce
        {
            get
            {
                float factor = 1f - 0.2f * GestationModules;
                return Props.fuelToProduce * Mathf.Max(0.1f, factor);
            }
        }

        /// <summary>Nombre de Sweepers produits par cycle (1 + modules multiplication).</summary>
        private int SweepersPerCycle => 1 + MultiplicationModules;

        public override void CompTick()
        {
            base.CompTick();
            if (!producing) return;

            // Tire du carburant du réseau
            float drawn = pipeNet?.DrawLiquid(Props.fuelDrawPerTick) ?? 0f;
            fuelAccumulated += drawn;

            // La progression avance proportionnellement au carburant accumulé
            progress = Mathf.Clamp01(fuelAccumulated / EffectiveFuelToProduce);

            if (progress >= 1f)
            {
                int count = SweepersPerCycle;
                for (int i = 0; i < count; i++)
                {
                    SpawnSweeper();
                }
                producing = false;
                progress = 0f;
                fuelAccumulated = 0f;
            }
        }

        /// <summary>
        /// Assigne une backstory à un Pawn_StoryTracker en tentant d'abord la
        /// propriété (nom majuscule), puis le champ (nom minuscule), via Traverse.
        /// Robuste face aux variations de nommage entre versions.
        /// </summary>
        private static void SetStoryField(Pawn_StoryTracker story, string propName,
            string fieldName, BackstoryDef value)
        {
            // Tente la propriété (ex: Childhood).
            Traverse prop = Traverse.Create(story).Property(propName);
            if (prop.PropertyExists())
            {
                prop.SetValue(value);
                Log.Message($"[ProjectMoon Sweepers] Backstory assignée via propriété {propName} = {value.defName}");
                return;
            }
            // Sinon, tente le champ (ex: childhood).
            Traverse field = Traverse.Create(story).Field(fieldName);
            if (field.FieldExists())
            {
                field.SetValue(value);
                Log.Message($"[ProjectMoon Sweepers] Backstory assignée via champ {fieldName} = {value.defName}");
                return;
            }
            Log.Warning($"[ProjectMoon Sweepers] Ni propriété '{propName}' ni champ '{fieldName}' trouvés sur Pawn_StoryTracker.");
        }

        private void SpawnSweeper()
        {
            Building building = parent as Building;
            if (building == null || !building.Spawned) return;

            // Génère un nouveau Sweeper (xénotype PM_Sweeper) dans la faction du joueur
            XenotypeDef sweeperXeno = DefDatabase<XenotypeDef>.GetNamedSilentFail("PM_Sweeper");
            PawnGenerationRequest request = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                allowFood: false,
                forcedXenotype: sweeperXeno,
                developmentalStages: DevelopmentalStage.Adult);

            Pawn newSweeper = PawnGenerator.GeneratePawn(request);

            // Backstories fixes des Sweepers issus de la Mother (enfance + adulte).
            // Le nom du champ/propriété varie (Childhood/childhood) selon la
            // version ; on tente les deux via Traverse pour être robuste.
            try
            {
                BackstoryDef childBg = DefDatabase<BackstoryDef>.GetNamedSilentFail("PM_Backstory_SweeperChild");
                BackstoryDef adultBg = DefDatabase<BackstoryDef>.GetNamedSilentFail("PM_Backstory_SweeperAdult");
                if (childBg == null || adultBg == null)
                {
                    Log.Warning($"[ProjectMoon Sweepers] BackstoryDef introuvable(s) : child={childBg?.defName ?? "NULL"}, adult={adultBg?.defName ?? "NULL"}");
                }
                if (newSweeper.story != null)
                {
                    if (childBg != null)
                        SetStoryField(newSweeper.story, "Childhood", "childhood", childBg);
                    if (adultBg != null)
                        SetStoryField(newSweeper.story, "Adulthood", "adulthood", adultBg);
                    newSweeper.Notify_DisabledWorkTypesChanged();
                }
            }
            catch (System.Exception e)
            {
                Log.Warning("[ProjectMoon Sweepers] Impossible d'assigner les backstories du Sweeper : " + e.Message);
            }

            // Le Sweeper produit hérite de l'idéologie primaire de la colonie.
            if (ModsConfig.IdeologyActive && newSweeper.ideo != null)
            {
                Ideo colonyIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
                if (colonyIdeo != null)
                {
                    newSweeper.ideo.SetIdeo(colonyIdeo);
                }
            }

            // Modules d'amélioration : +2 par module à chaque compétence (max +6).
            int enhance = EnhancementModules;
            if (enhance > 0 && newSweeper.skills != null)
            {
                int bonus = 2 * enhance;
                foreach (SkillRecord skill in newSweeper.skills.skills)
                {
                    if (skill == null || skill.TotallyDisabled) continue;
                    skill.Level = Mathf.Clamp(skill.Level + bonus, 0, 20);
                }
            }

            IntVec3 spawnCell = building.InteractionCell.IsValid
                ? building.InteractionCell
                : building.Position;
            GenSpawn.Spawn(newSweeper, spawnCell, building.Map);

            Messages.Message(
                $"La Mother a produit un nouveau Sweeper : {newSweeper.LabelShort}.",
                newSweeper, MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            if (!producing)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Produire un Sweeper",
                    defaultDesc = $"Lance la production autonome d'un nouveau Sweeper. Consomme {Props.fuelToProduce} de carburant vital depuis le réseau. La Mother travaille seule — aucun colon requis.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/PM_ProduceSweeper", false)
                           ?? BaseContent.BadTex,
                    action = () => { producing = true; }
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "Annuler la production",
                    defaultDesc = $"Progression actuelle : {Mathf.RoundToInt(progress * 100f)}%. Annuler perd le carburant déjà consommé.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/PM_CancelProduction", false)
                           ?? BaseContent.BadTex,
                    action = () =>
                    {
                        producing = false;
                        progress = 0f;
                        fuelAccumulated = 0f;
                    }
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            string netInfo = pipeNet != null
                ? $"Carburant vital réseau : {Mathf.FloorToInt(pipeNet.TotalStored)}"
                : "Non connectée au réseau";

            // Indicateurs chiffrés des effets cumulés des modules.
            int g = GestationModules, m = MultiplicationModules, e = EnhancementModules;

            // Vitesse : 100% de base, +% par module de gestation (via réduction de coût).
            // -20% de carburant ≈ +25% de vitesse effective ; on affiche le gain de coût.
            int speedPct = Mathf.RoundToInt(100f * (Props.fuelToProduce / EffectiveFuelToProduce));
            // Bonus de stats : +2 par module d'amélioration.
            int statBonus = 2 * e;

            string indicators =
                $"\nVitesse : {speedPct}% (gestation {g}/3)"
              + $"\nSweepers par cycle : {SweepersPerCycle}/4 (multiplication {m}/3)"
              + $"\nBonus de compétences : +{statBonus} (amélioration {e}/3)";

            if (producing)
                return $"Production en cours : {Mathf.RoundToInt(progress * 100f)}%"
                     + $"{indicators}\n{netInfo}";
            return $"Inactive — prête à produire.{indicators}\n{netInfo}";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref producing, "producing", false);
            Scribe_Values.Look(ref progress, "progress", 0f);
            Scribe_Values.Look(ref fuelAccumulated, "fuelAccumulated", 0f);
        }
    }
}
