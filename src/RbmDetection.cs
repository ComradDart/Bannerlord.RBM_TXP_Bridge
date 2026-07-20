using System.Reflection;
using HarmonyLib;

namespace RBM_TXP_Bridge
{
    /// <summary>
    /// Reads RBM's own config flag by reflection, so this module needs no
    /// compile-time reference to RBM (whose source is not public).
    ///
    /// Note: TXP itself checks "rbmAiEnabled" here, which is the wrong field --
    /// that is RBM's *combat AI* toggle, not its tournament toggle. The correct
    /// flag is "rbmTournamentEnabled". Both exist on RBMConfig.RBMConfig.
    /// </summary>
    internal static class RbmDetection
    {
        private const string ConfigTypeName = "RBMConfig.RBMConfig";
        private const string TournamentField = "rbmTournamentEnabled";

        private static FieldInfo _field;
        private static bool _resolved;

        public static bool RbmPresent { get; private set; }

        private static FieldInfo Field
        {
            get
            {
                if (!_resolved)
                {
                    _resolved = true;
                    var type = AccessTools.TypeByName(ConfigTypeName);
                    RbmPresent = type != null;
                    if (type != null)
                    {
                        _field = AccessTools.Field(type, TournamentField);
                        if (_field == null)
                            Log.Info($"WARN: found {ConfigTypeName} but not field '{TournamentField}'.");
                    }
                }
                return _field;
            }
        }

        /// <summary>
        /// Read live rather than cached: RBM's options can be toggled from the
        /// main menu between campaign loads.
        /// </summary>
        public static bool TournamentsActive
        {
            get
            {
                var field = Field;
                if (field == null) return false;
                try { return (bool)field.GetValue(null); }
                catch { return false; }
            }
        }
    }
}
