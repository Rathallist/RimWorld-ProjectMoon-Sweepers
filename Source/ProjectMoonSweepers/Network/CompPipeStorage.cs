using Verse;
using UnityEngine;

namespace ProjectMoonSweepers
{
    public class CompProperties_PipeStorage : CompProperties_PipeConnector
    {
        public float capacity = 200f;

        public CompProperties_PipeStorage()
        {
            this.compClass = typeof(CompPipeStorage);
        }
    }

    /// <summary>
    /// Stockage de Carburant Vital connecté au réseau (le réservoir).
    /// </summary>
    public class CompPipeStorage : CompPipeConnector
    {
        private float storedLiquid = 0f;

        public CompProperties_PipeStorage Props => (CompProperties_PipeStorage)props;
        public float StoredLiquid => storedLiquid;
        public float Capacity => Props.capacity;
        public bool IsFull => storedLiquid >= Props.capacity;

        public float AddLiquid(float amount)
        {
            float space = Props.capacity - storedLiquid;
            float accepted = Mathf.Min(amount, space);
            storedLiquid += accepted;
            return accepted;
        }

        public float DrawLiquid(float amount)
        {
            float drawn = Mathf.Min(amount, storedLiquid);
            storedLiquid -= drawn;
            return drawn;
        }

        // ── Accès au réseau (pour l'éjection au démantèlement) ────────────────
        public float PipeNetTotalStored => pipeNet?.TotalStored ?? storedLiquid;
        public float PipeNetTotalCapacity => pipeNet?.TotalCapacity ?? Props.capacity;

        /// <summary>Retire une quantité du réseau (réparti sur les stockages).</summary>
        public void DrawFromNet(float amount)
        {
            if (pipeNet != null)
                pipeNet.DrawLiquid(amount);
            else
                DrawLiquid(amount);
        }

        /// <summary>Ajoute du carburant au réseau, mais PAS à ce stockage-ci
        /// (utilisé au démantèlement pour redistribuer aux autres réservoirs).</summary>
        public void AddToNetExcludingSelf(float amount)
        {
            if (pipeNet == null || amount <= 0f) return;
            // Marque ce stockage comme plein temporairement en le retirant
            // logiquement : on ajoute au réseau qui répartit sur les autres.
            float before = storedLiquid;
            storedLiquid = Props.capacity; // bloque ce stockage
            pipeNet.AddLiquid(amount);
            storedLiquid = before; // restaure (ce réservoir part de toute façon)
        }

        public override string CompInspectStringExtra()
        {
            if (pipeNet != null)
                return $"Carburant vital (réseau) : {Mathf.FloorToInt(pipeNet.TotalStored)} / {Mathf.FloorToInt(pipeNet.TotalCapacity)}";
            return $"Carburant vital : {Mathf.FloorToInt(storedLiquid)} / {Mathf.FloorToInt(Props.capacity)}";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedLiquid, "storedLiquid", 0f);
        }
    }
}
