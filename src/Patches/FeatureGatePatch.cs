using System;
using System.Reflection;
using HarmonyLib;

namespace RBM_TXP_Bridge.Patches
{
    /// <summary>
    /// The heart of the bridge.
    ///
    /// TXP already has a per-tournament feature gate (TournamentCompatibilityService)
    /// used to stand down for TOR and Shokuho. RBM was never wired into it. Every
    /// contested TXP feature is gated through Supports(), so a single postfix that
    /// clears the contested flags makes TXP's own, already-shipping logic do the rest.
    ///
    /// We patch Supports() rather than GetProfile() because GetProfile returns
    /// TournamentCompatibilityProfile -- an internal type we cannot name at compile
    /// time. Supports() returns plain bool and takes the feature as an argument we
    /// can read boxed via __args.
    /// </summary>
    internal static class FeatureGatePatch
    {
        private const string ServiceType = "TournamentsXPanded.Compatibility.TournamentCompatibilityService";

        // Mirrors TournamentsXPanded.Compatibility.TournamentFeature (internal [Flags] enum).
        private const int PrizeAssignment = 1;
        private const int PrizeReroll = 2;
        private const int PrizeSelection = 4;
        private const int AdvancedPrizeGeneration = 8;

        /// <summary>
        /// Default is to strip NOTHING.
        ///
        /// The first cut of this bridge handed the whole prize pipeline to RBM, which
        /// did make the two coexist -- but it also killed the prize list and reroll,
        /// which are the main reasons to run TXP at all. It turned out unnecessary:
        /// RBM's tier rules are enforced in its GetTournamentPrize postfix, which runs
        /// regardless of what TXP does, and its GivePrizeToWinner awards whatever prize
        /// is set. So TXP can own selection while RBM still owns the item rules.
        /// See RbmPrizePinPatch for the piece that actually makes this work.
        ///
        /// DeferPrizesToRbm restores the old hand-everything-over behaviour as a
        /// fallback if the coexistence path misbehaves.
        /// </summary>
        private static int ContestedMask =>
            Settings.DeferPrizesToRbm
                ? (PrizeReroll | AdvancedPrizeGeneration | PrizeAssignment | PrizeSelection)
                : 0;

        private static bool _firstCallLogged;
        private static int _suppressionCount;

        public static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName(ServiceType);
            if (type == null)
            {
                Log.Info($"TXP not found ({ServiceType} unresolved) - bridge inactive.");
                return null;
            }
            var method = AccessTools.Method(type, "Supports");
            if (method == null)
                Log.Info($"WARN: {ServiceType} found but Supports() unresolved - TXP internals changed?");
            return method;
        }

        public static void Postfix(ref bool __result, object[] __args)
        {
            // Only ever narrows. Never turns a feature on that TXP had already declined.
            if (!__result) return;

            if (!_firstCallLogged)
            {
                _firstCallLogged = true;
                Log.Info("Supports() postfix is live (patch applied and not inlined away).");
                Diagnostics.DumpOnce("first tournament query");
            }

            if (!RbmDetection.TournamentsActive) return;

            int feature;
            try
            {
                // __args[1] is the boxed TournamentFeature; unbox as its underlying int.
                feature = Convert.ToInt32(__args[1]);
            }
            catch (Exception e)
            {
                Log.Info($"WARN: could not read feature argument: {e.Message}");
                return;
            }

            var mask = ContestedMask;
            if (mask == 0) return;

            if ((feature & mask) != 0)
            {
                __result = false;
                _suppressionCount++;
                if (_suppressionCount <= 10)
                    Log.Info($"Deferred feature {feature} to RBM (suppression #{_suppressionCount}).");
            }
        }
    }
}
