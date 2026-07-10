using Verse;

namespace ProjectMoonSweepers
{
    public class CompProperties_PipeConnector : CompProperties
    {
        public CompProperties_PipeConnector()
        {
            this.compClass = typeof(CompPipeConnector);
        }
    }

    /// <summary>
    /// Comp de base : marque un bâtiment comme connecté au réseau de tuyaux.
    /// Les tuyaux eux-mêmes n'ont que ce comp. Les producteurs/stockages/
    /// consommateurs ont des comps dérivés.
    /// </summary>
    public class CompPipeConnector : ThingComp
    {
        public PipeNet pipeNet;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            PipeNetManager mgr = parent.Map?.GetComponent<PipeNetManager>();
            mgr?.NotifyConnectorSpawned(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            PipeNetManager mgr = map?.GetComponent<PipeNetManager>();
            mgr?.NotifyConnectorDespawned(this);
            pipeNet = null;
        }
    }
}
