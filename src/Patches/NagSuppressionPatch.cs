using System.Reflection;
using HarmonyLib;

namespace RBM_TXP_Bridge.Patches
{
    /// <summary>
    /// TXP pops an inquiry telling the user to go disable RBM's tournament module.
    /// Once the bridge is active that advice is wrong -- the two coexist -- so the
    /// popup is suppressed.
    ///
    /// Worth noting the popup was misfiring anyway: it keys off RBMConfig.rbmAiEnabled
    /// (combat AI) instead of rbmTournamentEnabled, so it appeared for users who had
    /// already turned RBM tournaments off, and stayed silent for users who had them on
    /// with AI off. See RbmDetection.
    /// </summary>
    internal static class NagSuppressionPatch
    {
        private const string CheckerType = "TournamentsXPanded.Compatibility.RBM";

        public static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName(CheckerType);
            return type == null ? null : AccessTools.Method(type, "CheckAndTakeAction");
        }

        public static bool Prefix()
        {
            if (!Settings.SuppressRbmWarning) return true;
            if (!RbmDetection.TournamentsActive) return true;

            Log.Info("Suppressed TXP's 'disable RBM' popup (bridge is handling the conflict).");
            return false; // skip original
        }
    }
}
