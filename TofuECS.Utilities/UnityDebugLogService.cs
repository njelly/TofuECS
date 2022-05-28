using System;

namespace Tofunaut.TofuECS.Utilities
{
#if UNITY
    public class UnityDebugLogService : ILogService
    {
        public void Debug(string s)
        {
            UnityEngine.Debug.Log(s);
        }

        public void Info(string s)
        {
            UnityEngine.Debug.Log(s);
        }

        public void Warn(string s)
        {
            UnityEngine.Debug.LogWarning(s);
        }

        public void Error(string s)
        {
            UnityEngine.Debug.LogError(s);
        }

        public void Exception(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
    }
#endif
}