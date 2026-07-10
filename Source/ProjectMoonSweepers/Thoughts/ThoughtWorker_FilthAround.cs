using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Pensée situationnelle : malus de mood quand un croyant du précepte
    /// "saleté abhorrente" se trouve dans une pièce contenant de la saleté.
    /// L'intensité dépend de la quantité de saleté (3 paliers).
    ///
    /// Ce worker est référencé par le ThoughtDef PM_FilthAround_Mood, lui-même
    /// branché sur le précepte via PreceptComp_SituationalThought.
    /// RimWorld n'active la pensée que si le précepte est présent dans l'idéo
    /// du pawn (géré par le comp), donc on n'a pas à revérifier le précepte ici.
    /// </summary>
    public class ThoughtWorker_FilthAround : ThoughtWorker
    {
        // Seuils de quantité de saleté (nombre de filth dans la pièce).
        // 5 paliers progressifs.
        private const int Stage0Max = 3;   // 1-3   → très léger
        private const int Stage1Max = 8;   // 4-8   → léger
        private const int Stage2Max = 15;  // 9-15  → modéré
        private const int Stage3Max = 25;  // 16-25 → sévère
                                           // 26+   → extrême

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Pawn doit être éveillé, sur une map, capable de percevoir.
            if (p == null || !p.Spawned || p.Map == null)
                return ThoughtState.Inactive;

            // Les pawns qui dorment ne réagissent pas à la saleté.
            if (!p.Awake())
                return ThoughtState.Inactive;

            Room room = p.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                // Dehors : la saleté du sol naturel ne compte pas comme offense.
                return ThoughtState.Inactive;
            }

            int filthCount = CountFilthInRoom(room);
            if (filthCount <= 0)
                return ThoughtState.Inactive;

            if (filthCount <= Stage0Max)
                return ThoughtState.ActiveAtStage(0);
            if (filthCount <= Stage1Max)
                return ThoughtState.ActiveAtStage(1);
            if (filthCount <= Stage2Max)
                return ThoughtState.ActiveAtStage(2);
            if (filthCount <= Stage3Max)
                return ThoughtState.ActiveAtStage(3);
            return ThoughtState.ActiveAtStage(4);
        }

        /// <summary>
        /// Compte les objets de saleté (Filth) présents dans la pièce.
        /// Parcourt les cellules de la pièce et additionne les filth trouvées.
        /// </summary>
        private static int CountFilthInRoom(Room room)
        {
            int count = 0;
            Map map = room.Map;
            if (map == null) return 0;

            foreach (IntVec3 cell in room.Cells)
            {
                System.Collections.Generic.List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Filth)
                    {
                        count++;
                        // Petit raccourci : au-delà du seuil extrême, inutile de
                        // continuer à compter, le résultat sera le même.
                        if (count > Stage3Max + 1)
                            return count;
                    }
                }
            }
            return count;
        }
    }
}
