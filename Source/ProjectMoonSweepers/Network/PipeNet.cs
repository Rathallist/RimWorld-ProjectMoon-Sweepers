using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Un réseau de tuyaux de Carburant Vital. Regroupe tous les bâtiments
    /// (producteurs, stockage, consommateurs) connectés par des tuyaux.
    /// Le Carburant Vital est mis en commun : capacité et contenu partagés.
    /// </summary>
    public class PipeNet
    {
        public List<CompPipeConnector> connectors = new List<CompPipeConnector>();
        public Map map;

        public float TotalStored
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < connectors.Count; i++)
                {
                    CompPipeStorage s = connectors[i] as CompPipeStorage;
                    if (s != null) total += s.StoredLiquid;
                }
                return total;
            }
        }

        public float TotalCapacity
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < connectors.Count; i++)
                {
                    CompPipeStorage s = connectors[i] as CompPipeStorage;
                    if (s != null) total += s.Capacity;
                }
                return total;
            }
        }

        /// <summary>Ajoute du Carburant Vital au réseau, réparti dans les stockages. Retourne le surplus non stocké.</summary>
        public float AddLiquid(float amount)
        {
            if (amount <= 0f) return 0f;
            List<CompPipeStorage> storages = connectors.OfType<CompPipeStorage>()
                .Where(s => !s.IsFull).ToList();

            while (amount > 0.001f && storages.Count > 0)
            {
                float share = amount / storages.Count;
                float distributedThisRound = 0f;
                for (int i = storages.Count - 1; i >= 0; i--)
                {
                    float accepted = storages[i].AddLiquid(share);
                    distributedThisRound += accepted;
                    if (storages[i].IsFull) storages.RemoveAt(i);
                }
                amount -= distributedThisRound;
                if (distributedThisRound < 0.0001f) break;
            }
            return amount; // surplus
        }

        /// <summary>Retire du Carburant Vital du réseau. Retourne la quantité réellement obtenue.</summary>
        public float DrawLiquid(float amount)
        {
            if (amount <= 0f) return 0f;
            float drawn = 0f;
            List<CompPipeStorage> storages = connectors.OfType<CompPipeStorage>()
                .Where(s => s.StoredLiquid > 0f).ToList();

            foreach (CompPipeStorage s in storages)
            {
                float need = amount - drawn;
                if (need <= 0f) break;
                drawn += s.DrawLiquid(need);
            }
            return drawn;
        }
    }
}
