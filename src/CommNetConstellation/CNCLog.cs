using System;

namespace CommNetConstellation
{
    /// <summary>
    /// Debug-purpose logging
    /// </summary>
    public static class CNCLog
    {
        public const string NAME_LOG_PREFIX = "[CommNet Constellation]";

        public static void Info(string message, params object[] param)
        {
            UnityEngine.Debug.Log($"{NAME_LOG_PREFIX} {string.Format(message, param)}");
        }

        public static void Verbose(string message)
        {
            #if DEBUG
            UnityEngine.Debug.Log($"{NAME_LOG_PREFIX} Debug: {message}");
            #endif
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"{NAME_LOG_PREFIX} Error: {message}");
        }

        public static void Error(Exception e)
        {
            UnityEngine.Debug.LogError($"{NAME_LOG_PREFIX} Exception:\n{e}");
        }
    }
}