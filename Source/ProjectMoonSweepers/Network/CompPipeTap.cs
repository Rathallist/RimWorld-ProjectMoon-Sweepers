using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace ProjectMoonSweepers
{
    public class CompProperties_PipeTap : CompProperties_PipeConnector
    {
        public CompProperties_PipeTap()
        {
            this.compClass = typeof(CompPipeTap);
        }
    }

    /// <summary>
    /// Robinet : un Sweeper avec un faible niveau de Carburant Vital vient s'y
    /// alimenter, puisant dans le réseau pour remplir sa jauge interne (Need).
    /// Permet aussi de générer des bidons de carburant et un ordre manuel via clic-droit.
    /// </summary>
    public class CompPipeTap : CompPipeConnector
    {
        public float AvailableInNet => pipeNet?.TotalStored ?? 0f;

        public float DrawFromNet(float amount)
        {
            return pipeNet?.DrawLiquid(amount) ?? 0f;
        }

        public override string CompInspectStringExtra()
        {
            if (pipeNet != null)
                return $"Carburant vital disponible : {Mathf.FloorToInt(pipeNet.TotalStored)}";
            return "PM_Tap_NotConnected".Translate();
        }

        // ── Bouton : générer un bidon de carburant vital ──────────────────────
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            yield return new Command_Action
            {
                defaultLabel = "PM_Tap_ExtractLabel".Translate(),
                defaultDesc = "PM_Tap_ExtractDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Item/Resource/PM_RedLiquid", false)
                       ?? BaseContent.BadTex,
                action = () =>
                {
                    float available = AvailableInNet;
                    if (available < 25f)
                    {
                        Messages.Message("PM_Tap_NotEnough".Translate(),
                            MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    float drawn = DrawFromNet(25f);
                    int amount = Mathf.FloorToInt(drawn);
                    if (amount <= 0) return;

                    ThingDef liquidDef = DefDatabase<ThingDef>.GetNamedSilentFail("PM_RedLiquidResource");
                    if (liquidDef == null) return;
                    Thing liquid = ThingMaker.MakeThing(liquidDef);
                    liquid.stackCount = amount;
                    GenPlace.TryPlaceThing(liquid, parent.Position, parent.Map, ThingPlaceMode.Near);
                }
            };

            // Bouton : alimenter un Sweeper downed/alité (ciblage)
            yield return new Command_Action
            {
                defaultLabel = "PM_Tap_FeedLabel".Translate(),
                defaultDesc = "PM_Tap_FeedDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PM_ProduceSweeper", false)
                       ?? BaseContent.BadTex,
                action = () =>
                {
                    Find.Targeter.BeginTargeting(FeedTargetingParams(), (LocalTargetInfo target) =>
                    {
                        Pawn sweeper = target.Thing as Pawn;
                        if (sweeper == null) return;
                        // Trouve un colon libre pour faire le transfert
                        Pawn carrier = FindCarrier();
                        if (carrier == null)
                        {
                            Messages.Message("PM_Tap_NoColonist".Translate(),
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        Job job = JobMaker.MakeJob(
                            DefDatabase<JobDef>.GetNamed("PM_FeedFuelFromTap"), parent, sweeper);
                        carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
                }
            };
        }

        private TargetingParameters FeedTargetingParams()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                validator = (TargetInfo t) =>
                {
                    Pawn p = t.Thing as Pawn;
                    return p != null
                        && SweeperUtils.HasGene(p, "PM_LiquidBody")
                        && (p.Downed || p.InBed());
                }
            };
        }

        private Pawn FindCarrier()
        {
            foreach (Pawn p in parent.Map.mapPawns.FreeColonistsSpawned)
            {
                if (p != null && !p.Downed && !p.Dead
                    && p.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Some))
                    return p;
            }
            return null;
        }

        // ── Clic-droit : ordre manuel de boire ────────────────────────────────
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption o in base.CompFloatMenuOptions(selPawn))
                yield return o;

            if (!SweeperUtils.HasGene(selPawn, "PM_LiquidBody"))
                yield break;

            Need_RedLiquid need = selPawn.needs?.TryGetNeed<Need_RedLiquid>();
            if (need == null) yield break;

            if (AvailableInNet < 1f)
            {
                yield return new FloatMenuOption("PM_Tap_DrinkEmpty".Translate(), null);
                yield break;
            }

            if (need.CurLevel >= 1f)
            {
                yield return new FloatMenuOption("PM_Tap_DrinkFull".Translate(), null);
                yield break;
            }

            if (!selPawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("PM_Tap_DrinkUnreachable".Translate(), null);
                yield break;
            }

            yield return new FloatMenuOption("PM_Tap_Drink".Translate(), () =>
            {
                Job job = JobMaker.MakeJob(PM_JobDefOf.PM_DrinkFromTap, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
