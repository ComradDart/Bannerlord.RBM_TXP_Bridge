using System;
using System.Reflection;
using HarmonyLib;

namespace RBM_TXP_Bridge.Patches
{
    /// <summary>
    /// Makes TXP's prize pool and reroll work while keeping RBM's tier rules.
    ///
    /// Why this is needed:
    ///   TXP builds its prize list by calling UpdateTournamentPrize repeatedly and
    ///   harvesting a different Prize each time. RBM's UpdateTournamentPrize prefix
    ///   short-circuits whenever the current prize is already the correct tier, so
    ///   every call returns the same item, the pool dedupes down to one entry, and
    ///   reroll appears to do nothing.
    ///
    /// Why lifting the pin is safe:
    ///   RBM's tier enforcement does not live in that prefix. It lives in its
    ///   GetTournamentPrize postfix, which filters Items.All by player tier and town
    ///   culture and picks at random. That postfix still runs on every regeneration,
    ///   so each harvested candidate is still an RBM-legal, tier-correct, culture-
    ///   correct item. We get N RBM prizes instead of one.
    ///
    ///   And RBM's GivePrizeToWinner prefix awards tournament.Prize as-is -- it never
    ///   re-picks for the player -- so the prize TXP writes back is the prize awarded,
    ///   and RBM's positive-modifier ("legendary") roll still applies to it.
    ///
    /// Scope:
    ///   The pin is lifted only while TXP is actively generating candidates, so
    ///   ordinary gameplay keeps RBM's prize-stability behaviour untouched.
    /// </summary>
    internal static class RbmPrizePinPatch
    {
        private const string TxpService = "TournamentsXPanded.Compatibility.TournamentCompatibilityService";
        private const string TxpBehavior = "TournamentsXPanded.Behaviors.TournamentsXPandedBehavior";
        private const string RbmPatchType = "RBMTournament.RBMTournament+TournamentGamePatch";
        private const string RbmPrefixName = "UpdateTournamentPrizePrefix";

        [ThreadStatic] private static int _generationDepth;

        private static bool _loggedLift;

        public static bool InTxpGeneration => _generationDepth > 0;

        // --- scope markers -------------------------------------------------

        public static void EnterScope() => _generationDepth++;

        public static void ExitScope()
        {
            if (_generationDepth > 0) _generationDepth--;
        }

        // --- the actual override -------------------------------------------

        /// <summary>
        /// Prefix on RBM's own prefix. Forcing its return value to true means
        /// "do not skip the original", i.e. let the regeneration proceed.
        /// </summary>
        public static bool RbmPinPrefix(ref bool __result)
        {
            if (!InTxpGeneration) return true; // leave RBM alone

            if (!_loggedLift)
            {
                _loggedLift = true;
                Log.Info("Lifted RBM's prize pin during TXP candidate generation (first time).");
            }

            __result = true;  // let UpdateTournamentPrize run
            return false;     // skip RBM's own body
        }

        // --- target resolution ---------------------------------------------

        public static MethodBase RbmPinTarget()
        {
            var type = AccessTools.TypeByName(RbmPatchType) ?? FindNestedRbmPatchType();
            if (type == null)
            {
                // Expected at submodule load: RBM's tournament assembly is not loaded
                // that early. Only a real problem if it is still unresolved after
                // game init, which the caller reports.
                Log.Info($"{RbmPatchType} not resolvable yet; will retry after game init.");
                return null;
            }
            var method = AccessTools.Method(type, RbmPrefixName);
            if (method == null)
                Log.Info($"WARN: resolved {type.FullName} but not {RbmPrefixName}.");
            return method;
        }

        /// <summary>Fallback in case RBM's nested type naming shifts between releases.</summary>
        private static Type FindNestedRbmPatchType()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name?.IndexOf("RBMTournament", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name == "TournamentGamePatch" &&
                            AccessTools.Method(t, RbmPrefixName) != null)
                        {
                            Log.Info($"Resolved RBM patch type by fallback scan: {t.FullName}");
                            return t;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Info($"WARN: fallback scan failed: {e.Message}");
            }
            return null;
        }

        public static MethodBase GenerateScopeTarget()
        {
            var type = AccessTools.TypeByName(TxpService);
            return type == null ? null : AccessTools.Method(type, "GeneratePrizePreservingCurrent");
        }

        public static MethodBase RerollScopeTarget()
        {
            var type = AccessTools.TypeByName(TxpBehavior);
            return type == null ? null : AccessTools.Method(type, "RerollConsequence");
        }
    }
}
