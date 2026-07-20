using System;
using System.Reflection;
using HarmonyLib;
using RBM_TXP_Bridge.Patches;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RBM_TXP_Bridge
{
    public class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "com.rbmtxpbridge.patch";

        private Harmony _harmony;
        private bool _prizePinApplied;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Log.Reset();
            Log.Info("=== RBM x TXP Tournament Bridge v1.1.0 ===");
            Settings.Load();

            try
            {
                _harmony = new Harmony(HarmonyId);

                var rbm = RbmDetection.TournamentsActive;
                Log.Info($"RBM present: {RbmDetection.RbmPresent}; rbmTournamentEnabled: {rbm}");

                ApplyFeatureGate(_harmony);
                ApplyNagSuppression(_harmony);
                TryApplyPrizePin("submodule load");

                if (!RbmDetection.RbmPresent)
                    Log.Info("RBM absent - bridge loaded but dormant. Harmless.");

                if (!rbm && RbmDetection.RbmPresent)
                    Log.Info("NOTE: RBM is installed but its tournament module is OFF, so the bridge " +
                             "has nothing to reconcile. Enable it in RBM Options to test the bridge.");

                Diagnostics.DumpPatchState("submodule load");
            }
            catch (Exception e)
            {
                // Never take the game down over a compatibility shim.
                Log.Info($"ERROR during patching: {e}");
            }
        }

        /// <summary>
        /// RBM and TXP both register later than OnSubModuleLoad, so targets that were
        /// unresolvable at load time are retried here.
        /// </summary>
        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);

            try
            {
                TryApplyPrizePin("game initialized");
                Diagnostics.DumpPatchState("game initialized");
            }
            catch (Exception e)
            {
                Log.Info($"ERROR during late patching: {e}");
            }
        }

        private static void ApplyFeatureGate(Harmony harmony)
        {
            var target = FeatureGatePatch.TargetMethod();
            if (target == null)
            {
                Log.Info("Feature gate NOT applied (TXP not resolvable).");
                return;
            }

            var postfix = AccessTools.Method(typeof(FeatureGatePatch), nameof(FeatureGatePatch.Postfix));
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            Log.Info($"Feature gate applied to {target.DeclaringType?.FullName}.{target.Name}");
        }

        private static void ApplyNagSuppression(Harmony harmony)
        {
            var target = NagSuppressionPatch.TargetMethod();
            if (target == null)
            {
                Log.Info("Nag suppression NOT applied (TXP RBM checker not resolvable).");
                return;
            }

            var prefix = AccessTools.Method(typeof(NagSuppressionPatch), nameof(NagSuppressionPatch.Prefix));
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            Log.Info($"Nag suppression applied to {target.DeclaringType?.FullName}.{target.Name}");
        }

        /// <summary>
        /// The piece that actually restores TXP's prize pool and reroll under RBM.
        /// Idempotent: safe to call at both load and game-init.
        /// </summary>
        private void TryApplyPrizePin(string phase)
        {
            if (_prizePinApplied) return;

            if (!Settings.UnpinRbmPrizeForTxp)
            {
                Log.Info("Prize pin lift disabled by config.");
                _prizePinApplied = true;
                return;
            }

            var rbmTarget = RbmPrizePinPatch.RbmPinTarget();
            var genScope = RbmPrizePinPatch.GenerateScopeTarget();
            var rerollScope = RbmPrizePinPatch.RerollScopeTarget();

            if (rbmTarget == null || genScope == null)
            {
                var late = phase != "submodule load";
                Log.Info($"{(late ? "WARN: prize pin lift FAILED" : "Prize pin lift deferred")} at [{phase}] " +
                         $"(rbmTarget={(rbmTarget != null)}, genScope={(genScope != null)}).");
                return;
            }

            var enter = AccessTools.Method(typeof(RbmPrizePinPatch), nameof(RbmPrizePinPatch.EnterScope));
            var exit = AccessTools.Method(typeof(RbmPrizePinPatch), nameof(RbmPrizePinPatch.ExitScope));
            var pin = AccessTools.Method(typeof(RbmPrizePinPatch), nameof(RbmPrizePinPatch.RbmPinPrefix));

            // Mark the windows in which TXP is harvesting prize candidates...
            _harmony.Patch(genScope, prefix: new HarmonyMethod(enter), finalizer: new HarmonyMethod(exit));
            Log.Info($"Scope marker applied to {genScope.DeclaringType?.Name}.{genScope.Name}");

            if (rerollScope != null)
            {
                _harmony.Patch(rerollScope, prefix: new HarmonyMethod(enter), finalizer: new HarmonyMethod(exit));
                Log.Info($"Scope marker applied to {rerollScope.DeclaringType?.Name}.{rerollScope.Name}");
            }
            else
            {
                Log.Info("WARN: RerollConsequence not resolved; reroll button may still no-op.");
            }

            // ...and neutralise RBM's tier-pin inside those windows only.
            _harmony.Patch(rbmTarget, prefix: new HarmonyMethod(pin));
            Log.Info($"Prize pin lift applied to {rbmTarget.DeclaringType?.FullName}.{rbmTarget.Name} at [{phase}]");

            _prizePinApplied = true;
        }
    }
}
