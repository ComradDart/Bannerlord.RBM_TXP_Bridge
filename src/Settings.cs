using System;
using System.IO;

namespace RBM_TXP_Bridge
{
    /// <summary>
    /// Plain-file settings rather than MCM. MCM would add a hard version dependency
    /// for two booleans, and this module has to stay loadable even when TXP's own
    /// MCM registration is in flux.
    ///
    /// Config lives next to the log:
    ///   Documents\Mount and Blade II Bannerlord\Logs\RBM_TXP_Bridge.cfg
    /// Format: one "key=value" per line. Missing file means defaults.
    /// </summary>
    internal static class Settings
    {
        /// <summary>Suppress TXP's "go disable RBM" inquiry popup.</summary>
        public static bool SuppressRbmWarning = true;

        /// <summary>
        /// Lift RBM's prize pin while TXP harvests candidates, so the prize pool and
        /// reroll actually work. This is the point of the bridge; off means TXP shows
        /// a single prize and reroll does nothing.
        /// </summary>
        public static bool UnpinRbmPrizeForTxp = true;

        /// <summary>
        /// Fallback: hand the entire prize pipeline to RBM (no pool, no reroll, no
        /// selection). Only useful if coexistence turns out to misbehave.
        /// </summary>
        public static bool DeferPrizesToRbm = false;

        public static void Load()
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord", "Logs", "RBM_TXP_Bridge.cfg");

                if (!File.Exists(path))
                {
                    Log.Info("No config file; using defaults.");
                    return;
                }

                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;

                    var split = line.IndexOf('=');
                    if (split <= 0) continue;

                    var key = line.Substring(0, split).Trim();
                    var value = line.Substring(split + 1).Trim();
                    if (!bool.TryParse(value, out var parsed)) continue;

                    if (key.Equals("SuppressRbmWarning", StringComparison.OrdinalIgnoreCase))
                        SuppressRbmWarning = parsed;
                    else if (key.Equals("UnpinRbmPrizeForTxp", StringComparison.OrdinalIgnoreCase))
                        UnpinRbmPrizeForTxp = parsed;
                    else if (key.Equals("DeferPrizesToRbm", StringComparison.OrdinalIgnoreCase))
                        DeferPrizesToRbm = parsed;
                }

                Log.Info($"Config: SuppressRbmWarning={SuppressRbmWarning}, " +
                         $"UnpinRbmPrizeForTxp={UnpinRbmPrizeForTxp}, DeferPrizesToRbm={DeferPrizesToRbm}");
            }
            catch (Exception e)
            {
                Log.Info($"WARN: config load failed ({e.Message}); using defaults.");
            }
        }
    }
}
