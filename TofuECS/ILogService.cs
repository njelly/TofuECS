using System;

namespace Tofunaut.TofuECS
{
    public interface ILogService
    {
        void Debug(string s);
        void Info(string s);
        void Warn(string s);
        void Error(string s);
        void Exception(Exception e);
    }
}