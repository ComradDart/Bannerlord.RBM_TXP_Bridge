using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RBM_TXP_Bridge
{
    /// <summary>
    /// Dumps the live Harmony patch state for every method RBM and TXP contest.
    /// This exists so "does the bridge work?" is answered by evidence rather than
    /// by whether a tournament subjectively felt right.
    ///
    /// Methods are resolved by name so no extra assembly references are needed.
    /// </summary>
    internal static class Diagnostics
    {
        private static readonly string[] ContestedMethods =
        {
            // Contested: RBM prefix (skip-original) vs TXP postfix.
            "TaleWorlds.CampaignSystem.TournamentGames.TournamentGame:UpdateTournamentPrize",
            // Contested: RBM postfix vs TXP's advanced prize generation.
            "TaleWorlds.CampaignSystem.TournamentGames.FightTournamentGame:GetTournamentPrize",
            // RBM replaces outright.
            "TaleWorlds.CampaignSystem.TournamentGames.FightTournamentGame:GetParticipantCharacters",
            "TaleWorlds.CampaignSystem.TournamentGames.TournamentManager:GivePrizeToWinner",
            // Uncontested, listed to confirm TXP keeps them.
            "TaleWorlds.CampaignSystem.TournamentGames.FightTournamentGame:GetMenuText",
            // RBM owns match simulation; TXP's controller is additive and should not appear.
            "SandBox.Tournaments.MissionLogics.TournamentFightMissionController:Simulate",
        };

        private static bool _dumped;

        public static void DumpPatchState(string phase)
        {
            try
            {
                Log.Info($"--- Harmony patch state [{phase}] ---");
                foreach (var id in ContestedMethods)
                {
                    MethodBase target = null;
                    try { target = AccessTools.Method(id); } catch { }

                    if (target == null)
                    {
                        Log.Info($"  {Short(id)}: METHOD NOT RESOLVED");
                        continue;
                    }

                    var info = Harmony.GetPatchInfo(target);
                    if (info == null)
                    {
                        Log.Info($"  {Short(id)}: unpatched");
                        continue;
                    }

                    Log.Info($"  {Short(id)}:");
                    Describe("prefix", info.Prefixes);
                    Describe("postfix", info.Postfixes);
                    Describe("transpiler", info.Transpilers);
                }
                Log.Info("--- end patch state ---");
            }
            catch (Exception e)
            {
                Log.Info($"WARN: diagnostics failed: {e.Message}");
            }
        }

        /// <summary>Called once from the feature gate, when tournament code is genuinely running.</summary>
        public static void DumpOnce(string phase)
        {
            if (_dumped) return;
            _dumped = true;
            DumpPatchState(phase);
        }

        private static void Describe(string kind, System.Collections.Generic.IList<Patch> patches)
        {
            if (patches == null || patches.Count == 0) return;
            foreach (var p in patches.OrderBy(x => x.priority))
                Log.Info($"      {kind}: owner={p.owner} prio={p.priority} method={p.PatchMethod?.DeclaringType?.Name}.{p.PatchMethod?.Name}");
        }

        private static string Short(string id)
        {
            var colon = id.IndexOf(':');
            if (colon < 0) return id;
            var type = id.Substring(0, colon);
            var dot = type.LastIndexOf('.');
            return (dot < 0 ? type : type.Substring(dot + 1)) + id.Substring(colon);
        }
    }
}
