using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace ProjectMoonSweepers
{
    public class CompProperties_CrystallizerFuel : CompProperties_PipeConnector
    {
        public CompProperties_CrystallizerFuel()
        {
            this.compClass = typeof(CompCrystallizerFuel);
        }
    }

    /// <summary>
    /// Connecte le Cristalliseur au réseau de carburant vital et expose
    /// la quantité disponible. La consommation réelle se fait à la complétion
    /// d'un bill via le patch Patch_CrystallizerRecipe.
    /// </summary>
    public class CompCrystallizerFuel : CompPipeConnector
    {
        public float AvailableFuel => pipeNet?.TotalStored ?? 0f;

        public bool TryConsumeFuel(float amount)
        {
            if (pipeNet == null) return false;
            if (pipeNet.TotalStored < amount) return false;
            pipeNet.DrawLiquid(amount);
            return true;
        }

        public override string CompInspectStringExtra()
        {
            if (pipeNet != null)
                return $"Carburant vital réseau : {Mathf.FloorToInt(pipeNet.TotalStored)}";
            return "Non connecté au réseau de carburant vital";
        }
    }
}
