using System;

namespace CommNetConstellation
{
    /// <summary>
    /// Debug-purpose logging
    /// </summary>
    public static class CNCLog
    {
        public static readonly string NAME_LOG_PREFIX = "CommNet Constellation";

        public static void Verbose(string message, params object[] param)
        {
            UnityEngine.Debug.Log($"[{NAME_LOG_PREFIX}] {string.Format(message, param)}");
        }

        public static void Debug(string message, params object[] param)
        {
            #if DEBUG
            UnityEngine.Debug.Log(string.Format("[{0}] Debug: {1}", NAME_LOG_PREFIX, string.Format(message, param)));
            #endif
        }

        public static void Error(string message, params object[] param)
        {
            UnityEngine.Debug.LogError($"[{NAME_LOG_PREFIX}] Error: {string.Format(message, param)}");
        }

        public static void Error(Exception ex)
        {
            #if DEBUG
            UnityEngine.Debug.LogException(ex);
            #endif
        }
    }
}