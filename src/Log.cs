using System;
using System.IO;

namespace RBM_TXP_Bridge
{
    /// <summary>
    /// Deliberately file-based. The whole point of this module is verifying that
    /// patches against two closed-source assemblies actually took effect, and an
    /// in-game message is too easy to miss during a tournament.
    /// </summary>
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static string _path;

        private static string Path
        {
            get
            {
                if (_path == null)
                {
                    var dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Mount and Blade II Bannerlord", "Logs");
                    try { Directory.CreateDirectory(dir); } catch { /* fall through */ }
                    _path = System.IO.Path.Combine(dir, "RBM_TXP_Bridge.log");
                }
                return _path;
            }
        }

        public static void Info(string message)
        {
            lock (Gate)
            {
                try
                {
                    File.AppendAllText(Path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                }
                catch
                {
                    // Logging must never take the game down.
                }
            }
        }

        public static void Reset()
        {
            lock (Gate)
            {
                try { if (File.Exists(Path)) File.Delete(Path); } catch { }
            }
        }
    }
}
