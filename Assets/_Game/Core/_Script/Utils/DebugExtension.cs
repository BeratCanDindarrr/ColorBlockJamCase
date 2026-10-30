using System;
using System.Diagnostics;
using Object = UnityEngine.Object;

namespace ColorBlockJam.Utils
{
    // Calls are stripped by the compiler (arguments included) unless the DEV scripting define is set.
    public static class DebugExtension
    {
        private const string DevDefine = "DEV";

        [Conditional(DevDefine)]
        public static void Log(object message, Object context = null)
        {
            UnityEngine.Debug.Log(message, context);
        }

        [Conditional(DevDefine)]
        public static void LogWarning(object message, Object context = null)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        [Conditional(DevDefine)]
        public static void LogError(object message, Object context = null)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        [Conditional(DevDefine)]
        public static void LogException(Exception exception, Object context = null)
        {
            UnityEngine.Debug.LogException(exception, context);
        }
    }
}
