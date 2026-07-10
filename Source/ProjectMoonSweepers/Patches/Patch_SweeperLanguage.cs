using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMoonSweepers
{
    /// <summary>
    /// Barrière linguistique : sans la recherche PM_Research_Translation, un
    /// colon et un marchand-pawn qui ne partagent pas la même "langue" (l'un
    /// Sweeper, l'autre non) ne peuvent pas commercer. Une tentative affiche un
    /// message chiffré et la fenêtre de commerce se referme immédiatement.
    ///
    /// On patche Dialog_Trade.PostOpen (appelée à l'ouverture réelle de la
    /// fenêtre de commerce) : si la barrière s'applique, on ferme la fenêtre et
    /// on affiche le message. C'est le point fiable — TradeSession.SetupWith
    /// prépare la session mais n'ouvre pas la fenêtre.
    /// Enregistré manuellement depuis ModInit.
    /// </summary>
    public static class SweeperLanguage
    {
        private const string TranslationResearchDefName = "PM_Research_Translation";

        public static bool TranslationResearched()
        {
            ResearchProjectDef def =
                DefDatabase<ResearchProjectDef>.GetNamedSilentFail(TranslationResearchDefName);
            return def != null && def.IsFinished;
        }

        public static bool CanCommunicate(Pawn a, Pawn b)
        {
            if (a == null || b == null) return true;
            if (TranslationResearched()) return true;
            return SweeperUtils.IsSweeper(a) == SweeperUtils.IsSweeper(b);
        }

        public static string GarbledMessage(Pawn trader)
        {
            StringBuilder sb = new StringBuilder();
            int groups = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < groups; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(Rand.RangeInclusive(100, 999999).ToString());
            }
            string traderName = trader?.LabelShort ?? "PM_Lang_Someone".Translate().ToString();
            return sb.ToString() + "  —  " + traderName
                + "PM_Lang_Garbled".Translate();
        }
    }

    /// <summary>
    /// Prefix sur Dialog_Trade.PostOpen. Si la barrière linguistique s'applique
    /// entre le négociateur et le marchand-pawn de la session, on affiche le
    /// message chiffré et on ferme la fenêtre immédiatement.
    /// Enregistré manuellement depuis ModInit.
    /// </summary>
    public static class Patch_Dialog_Trade_PostOpen
    {
        public static void Postfix(Dialog_Trade __instance)
        {
            // Récupère la session active.
            Pawn negotiator = TradeSession.playerNegotiator;
            Pawn traderPawn = TradeSession.trader as Pawn;

            if (negotiator == null || traderPawn == null)
                return;

            if (SweeperLanguage.CanCommunicate(negotiator, traderPawn))
                return;

            Messages.Message(
                SweeperLanguage.GarbledMessage(traderPawn),
                traderPawn,
                MessageTypeDefOf.RejectInput,
                historical: false);

            // Ferme la fenêtre de commerce qui vient de s'ouvrir.
            __instance.Close(doCloseSound: false);
        }
    }
}
